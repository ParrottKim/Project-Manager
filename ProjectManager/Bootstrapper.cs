using Caliburn.Micro;
using ProjectManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ProjectManager
{
    public class Bootstrapper : BootstrapperBase
    {
        private SimpleContainer _container = new SimpleContainer();

        public Bootstrapper()
        {
            Initialize();
        }

        protected override void Configure()
        {
            _container
                .Singleton<IWindowManager, WindowManager>()
                .Singleton<IEventAggregator, EventAggregator>();

            GetType().Assembly.GetTypes()
                .Where(type => type.IsClass)
                .Where(type => type.Name.EndsWith("ViewModel"))
                .ToList()
                .ForEach(viewModelType => _container.RegisterPerRequest(
                    viewModelType, viewModelType.ToString(), viewModelType));

            //GetType().Assembly.GetTypes()
            //    .Where(type => type.IsClass)
            //    .Where(type => type.Name.EndsWith("ViewModel"))
            //    .ToList()
            //    .ForEach(viewModelType => Console.WriteLine(viewModelType.ToString()));

            //_container = new SimpleContainer();

            //_container.Singleton<IWindowManager, WindowManager>();

            //_container.PerRequest<DashboardViewModel>();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            Application.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var windowManager = IoC.Get<IWindowManager>();
            var eventAggregator = IoC.Get<IEventAggregator>();

            //windowManager.ShowDialog(new SplashViewModel(eventAggregator));
            DisplayRootViewFor<DashboardViewModel>();
        }

        protected override object GetInstance(Type service, string key)
        {
            return _container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            _container.BuildUp(instance);
        }
    }
}