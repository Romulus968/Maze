﻿using ModuleTemplate.Administration.Resources;
using ModuleTemplate.Administration.ViewModels;
using Orcus.Administration.Library.Menus;
using Orcus.Administration.Library.Models;
using Orcus.Administration.Library.Services;
using Prism.Modularity;
using Unclassified.TxLib;

namespace ModuleTemplate.Administration
{
    public class PrismModule : IModule
    {
        private readonly VisualStudioIcons _icons;
        private readonly IClientCommandRegistrar _registrar;

        public PrismModule(IClientCommandRegistrar registrar, VisualStudioIcons icons)
        {
            _registrar = registrar;
            _icons = icons;
        }

        public void Initialize()
        {
            Tx.LoadFromEmbeddedResource("$safeprojectname$.Resources.Module.Translation.txd");

            _registrar.Register<ModuleViewModel>("Module:Name", IconFactory.FromFactory(() => _icons.Icon), CommandCategory.System);
        }
    }
}