using System.Windows;
using System.Reflection;

namespace LuAnimatorV2
{
    /// <summary>
    /// Логика взаимодействия для About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();

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

            Title = System.String.Format("About {0}", title.Title);
            lblName.Text = title.Title;
            lblDescription.Text = description.Description;
            lblVersion.Text = System.String.Format("Version {0}", version.ToString());
            lblCopyright.Text = copyright.Copyright.ToString();
            lblCompany.Text = company.Company;
            lblDisclaimer.Text = "All right reserved.";
        }
    }
}
