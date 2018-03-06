using System.Windows;
using System.Reflection;
using System.Windows.Controls;

namespace LuAnimatorV2
{
    /// <summary>
    /// Interaction logic for AboutWindow
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            FillInInfo();
        }

        private void FillInInfo()
        {
            Assembly app = Assembly.GetExecutingAssembly();

            AssemblyTitleAttribute title = (AssemblyTitleAttribute)app.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0];
            AssemblyProductAttribute product = (AssemblyProductAttribute)app.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0];
            AssemblyCopyrightAttribute copyright = (AssemblyCopyrightAttribute)app.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0];
            AssemblyCompanyAttribute company = (AssemblyCompanyAttribute)app.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false)[0];
            AssemblyDescriptionAttribute description = (AssemblyDescriptionAttribute)app.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)[0];

            System.Version version = app.GetName().Version;

            if (System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
            {
                version = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }

            SetCurrentValue(TitleProperty, System.String.Format("About {0}", title.Title));
            lblName.SetCurrentValue(TextBlock.TextProperty, title.Title);
            lblDescription.SetCurrentValue(TextBlock.TextProperty, description.Description);
            lblVersion.SetCurrentValue(TextBlock.TextProperty, System.String.Format("Version {0}", version.ToString()));
            lblCopyright.SetCurrentValue(TextBlock.TextProperty, copyright.Copyright.ToString());
            lblCompany.SetCurrentValue(TextBlock.TextProperty, company.Company);
            lblDisclaimer.SetCurrentValue(TextBlock.TextProperty, "All rights reserved.");
        }
    }
}
