using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Threading;

namespace AdServices___Client
{
    public partial class Form1 : Form
    {
        private List<string> errors;
        private IPAddress server;
        Network network;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_FormClosed(Object sender, FormClosedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //label4.Hide();
            this.FormClosed += new FormClosedEventHandler(Form1_FormClosed);
            emailtext.Text = "demantor@live.com";
            passwordtext.Text = "yoloyolo";
            ipaddresstext.Text = "127.0.0.1";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private bool Validate_Input()
        {
            this.errors = new List<string>();
            return new bool [] { isValid_email(), isValid_password(), isValid_ipaddress() }.ToList<bool>().TrueForAll(_ => _);
        }

        private bool isValid_email()
        {
            string email = emailtext.Text;
            if (email == null || email == "")
            {
                errors.Add("Enter your Email");
                return false;
            }

            else if(email.Length < 4 || !email.Contains("@"))
            {
                errors.Add("Enter a Valid Email");
                return false;
            }


            try
            {
                MailAddress mail = new MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                errors.Add("Invalid Email");
                return false;
            }
        }

        private bool isValid_password()
        {
            string password = passwordtext.Text;
            if(password == null || password == "")
            {
                errors.Add("Enter your Password");
                return false;
            }

            else if (password.Length < 6)
            {
                errors.Add("Enter a bigger Password");
                return false;
            }
            return true;
        }

        private bool isValid_ipaddress()
        {
            string ip = ipaddresstext.Text;
            if (ip == null || ip == "")
            {
                errors.Add("Enter an IPAddress");
                return false;
            }

            if (!System.Net.IPAddress.TryParse(ip, out server))
            {
                errors.Add("Enter a Valid IPAddress");
                return false;
            }
            return true;
        }

        delegate void postMsgD(string text);

        public void postMsg(string text)
        {
            if (label4.InvokeRequired)
            {
                label4.Invoke(new postMsgD(this.postMsg), new object[] { text });
            }
            else
            {
                label4.Text = text;
            }
        }

        private void _postError()
        {
            clearError();
            StringBuilder str = new StringBuilder();
            errors.ForEach(_ => str.Append(_).Append("\n"));
            label4.Text = str.ToString();
        }

        private void postErrorLocal(string err)
        {
            clearError();
            label4.Text = err;
        }

        public void clearError()
        {
            label4.ResetText();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(!Validate_Input())
            {
                _postError();
                return;
            }
            LockMe();
            postErrorLocal("Connecting..");

            network = new Network(server, this);
            network.connect();
        }

        delegate void LockMeD();

        public void LockMe()
        {
            if (groupBox1.InvokeRequired)
            {
                groupBox1.Invoke(new LockMeD(this.LockMe));
            }
            else
                groupBox1.Enabled = false;
        }

        delegate void UnLockMeD();

        public void UnLockMe()
        {
            if (groupBox1.InvokeRequired)
            {
                groupBox1.Invoke(new UnLockMeD(this.UnLockMe));
            }
            else
                groupBox1.Enabled = true;
        }
    }
}
