using System;
using System.Drawing;
using System.Security.Principal;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.UI.Forms;
using WixToolset.Dtf.WindowsInstaller;

namespace WindowsIplmClientInstaller.Dialogs
{
    /// <summary>
    /// The standard Installation Progress dialog
    /// </summary>
    public partial class ProgressDialog : ManagedForm, IManagedDialog, IProgressDialog // change ManagedForm->Form if you want to show it in designer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialog"/> class.
        /// </summary>
        public ProgressDialog()
        {
            InitializeComponent();
            dialogText.MakeTransparentOn(banner);

            showWaitPromptTimer = new System.Windows.Forms.Timer() { Interval = 4000 };
            showWaitPromptTimer.Tick += (s, e) =>
            {
                this.waitPrompt.Visible = true;
                showWaitPromptTimer.Stop();
            };
        }

        System.Windows.Forms.Timer showWaitPromptTimer;

        void ProgressDialog_Load(object sender, EventArgs e)
        {
            banner.Image = Runtime.Session.GetResourceBitmap("WixUI_Bmp_Banner") ??
                           Runtime.Session.GetResourceBitmap("WixSharpUI_Bmp_Banner");

            if (!WindowsIdentity.GetCurrent().IsAdmin() && Uac.IsEnabled())
            {
                this.waitPrompt.Text = Runtime.Session.Property("UAC_WARNING");

                showWaitPromptTimer.Start();
            }

            if (banner.Image != null)
                ResetLayout();

            Shell.StartExecute();
        }

        void ResetLayout()
        {
            // The form controls are properly anchored and will be correctly resized on parent form
            // resizing. However the initial sizing by WinForm runtime doesn't a do good job with DPI
            // other than 96. Thus manual resizing is the only reliable option apart from going WPF.
            float ratio = (float)banner.Image.Width / (float)banner.Image.Height;
            topPanel.Height = (int)(banner.Width / ratio);
            topBorder.Top = topPanel.Height + 1;

            var upShift = (int)(next.Height * 2.3) - bottomPanel.Height;
            bottomPanel.Top -= upShift;
            bottomPanel.Height += upShift;

            var fontSize = waitPrompt.Font.Size;
            float scaling = 1;
            waitPrompt.Font = new Font(waitPrompt.Font.Name, fontSize * scaling, FontStyle.Italic);
        }

        /// <summary>
        /// Called when Shell is changed. It is a good place to initialize the dialog to reflect the MSI session
        /// (e.g. localize the view).
        /// </summary>
        protected override void OnShellChanged()
        {
            if (Runtime.Session.IsUninstalling())
            {
                dialogText.Text =
                Text = "[ProgressDlgTitleRemoving]";
                description.Text = "[ProgressDlgTextRemoving]";
            }
            else if (Runtime.Session.IsRepairing())
            {
                dialogText.Text =
                Text = "[ProgressDlgTextRepairing]";
                description.Text = "[ProgressDlgTitleRepairing]";
            }
            else if (Runtime.Session.IsInstalling())
            {
                dialogText.Text =
                Text = "[ProgressDlgTitleInstalling]";
                description.Text = "[ProgressDlgTextInstalling]";
            }

            this.Localize();
        }

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="messageRecord">The message record.</param>
        /// <param name="buttons">The buttons.</param>
        /// <param name="icon">The icon.</param>
        /// <param name="defaultButton">The default button.</param>
        /// <returns></returns>
        public override MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord, MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton)
        {
            switch (messageType)
            {
                case InstallMessage.InstallStart:
                case InstallMessage.InstallEnd:
                    {
                        showWaitPromptTimer.Stop();
                        waitPrompt.Visible = false;
                    }
                    break;

                case InstallMessage.ActionStart:
                    {
                        try
                        {
                            // see Discussion: https://github.com/oleg-shilo/wixsharp/discussions/1504
                            //messageRecord[0] - is reserved for FormatString value

                            string message = null;

                            bool simple = true;
                            if (simple)
                            {
                                /*
                                messageRecord[2] unconditionally contains the string to display

                                Examples:

                                   messageRecord[0]    "Action 23:14:50: [1]. [2]"
                                   messageRecord[1]    "InstallFiles"
                                   messageRecord[2]    "Copying new files"
                                   messageRecord[3]    "File: [1],  Directory: [9],  Size: [6]"

                                   messageRecord[0]    "Action 23:15:21: [1]. [2]"
                                   messageRecord[1]    "RegisterUser"
                                   messageRecord[2]    "Registering user"
                                   messageRecord[3]    "[1]"

                                */
                                if (messageRecord.FieldCount >= 3)
                                {
                                    message = messageRecord[2].ToString();
                                }
                            }
                            else
                            {
                                message = messageRecord.FormatString;
                                if (message.IsNotEmpty())
                                {
                                    for (int i = 1; i < messageRecord.FieldCount; i++)
                                    {
                                        message = message.Replace("[" + i + "]", messageRecord[i].ToString());
                                    }
                                }
                                else
                                {
                                    message = messageRecord[messageRecord.FieldCount - 1].ToString();
                                }
                            }

                            if (message.IsNotEmpty())
                                currentAction.Text = "{0} {1}".FormatWith(currentActionLabel.Text, message);
                        }
                        catch
                        {
                            //Catch all, we don't want the installer to crash in an
                            //attempt to process message.
                        }
                    }
                    break;
            }
            return MessageResult.OK;
        }

        /// <summary>
        /// Called when MSI execution progress is changed.
        /// </summary>
        /// <param name="progressPercentage">The progress percentage.</param>
        public override void OnProgress(int progressPercentage)
        {
            progress.Value = progressPercentage;

            if (progressPercentage > 0)
            {
                waitPrompt.Visible = false;
            }
        }

        /// <summary>
        /// Called when MSI execution is complete.
        /// </summary>
        public override void OnExecuteComplete()
        {
            currentAction.Text = null;
            Shell.GoNext();
        }

        /// <summary>
        /// Handles the Click event of the cancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        void cancel_Click(object sender, EventArgs e)
        {
            if (Shell.IsDemoMode)
                Shell.GoNext();
            else
                Shell.Cancel();
        }
    }
}