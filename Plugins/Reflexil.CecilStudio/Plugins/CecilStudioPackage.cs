/*
    Reflexil .NET assembly editor.
    Copyright (C) 2007-2009 Sebastien LEBRETON

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

#region " Imports "
using System;
using System.Collections;
using System.Linq;
using Cecil.Decompiler.Gui.Services;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using Reflexil.Utils;
#endregion

namespace Reflexil.Plugins.CecilStudio
{
	/// <summary>
	/// Addin entry point
	/// </summary>
    public class CecilStudioPackage : BasePackage, Cecil.Decompiler.Gui.Services.IPlugin
    {

        #region " Constants "
        const string CECILSTUDIO_RESOURCE_IMAGES = "Cecil.Decompiler.Gui.icons.png";
        #endregion

        #region " Fields "
        private IWindowManager wm;
		private IAssemblyBrowser ab;
		private IBarManager cbm;
		private IAssemblyManager am;
		private IServiceProvider sp;
        private List<UIContext> items;
		#endregion

        #region " Properties "
        public override ICollection Assemblies
        {
            get { return Enumerable.ToList(am.Assemblies); }
        }

        public override object ActiveItem
        {
            get { return ab.ActiveItem; }
        }
        #endregion

        #region " Events "
        /// <summary>
        /// 'Reflexil' button click 
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event parameters</param>
		protected override void Button_Click(object sender, EventArgs e)
		{
			wm.Windows[REFLEXIL_WINDOW_ID].Visible = true;
		}
		#endregion
		
		#region " Methods "
        /// <summary>
        /// Display a message
        /// </summary>
        /// <param name="message">message to display</param>
        public override void ShowMessage(string message)
        {
            wm.ShowMessage(message);
        }

        /// <summary>
        /// Helper method
        /// </summary>
        /// <typeparam name="T">Cecil Studio service interface</typeparam>
        /// <returns>Cecil studio service implementation</returns>
		public T GetService<T>()
		{
			return ((T) (sp.GetService(typeof(T))));
		}

        /// <summary>
        /// Add a menu
        /// </summary>
        /// <param name="id">Menu id</param>
        /// <returns>a menu context</returns>
        private MenuUIContext AddMenu(string id)
        {
            items.Add(new MenuUIContext(cbm.Bars[id]));
            return new MenuUIContext(cbm.Bars[id], GenerateId(id), REFLEXIL_BUTTON_TEXT, BasePlugin.ReflexilImage);
        }

        /// <summary>
        /// Addin load method
        /// </summary>
        /// <param name="serviceProvider">Cecil Studio service provider</param>
		public void Load(System.IServiceProvider serviceProvider)
		{
            PluginFactory.Register(new CecilStudioPlugin(this));
            
            sp = serviceProvider;
			wm = GetService<IWindowManager>();
			ab = GetService<IAssemblyBrowser>();
			cbm = GetService<IBarManager>();
			am = GetService<IAssemblyManager>();

            CheckFrameWorkVersion();

            // Main Window
            items = new List<UIContext>();
            reflexilwindow = new Reflexil.Forms.ReflexilWindow();
            IWindow window = wm.Windows.Add(REFLEXIL_WINDOW_ID, reflexilwindow, REFLEXIL_WINDOW_TEXT);
            window.Image = BasePlugin.ReflexilImage;

            // Main button
            items.Add(new ButtonUIContext(cbm.Bars[BarNames.Toolbar]));
            items.Add(new ButtonUIContext(cbm.Bars[BarNames.Toolbar], REFLEXIL_BUTTON_TEXT, Button_Click, BasePlugin.ReflexilImage));

            using (ImageList browserimages = new ImageList())
            {
                browserimages.Images.AddStrip(PluginFactory.GetInstance().GetAllBrowserImages());
                browserimages.TransparentColor = Color.Green;

                using (ImageList barimages = new ImageList())
                {
                    barimages.Images.AddStrip(PluginFactory.GetInstance().GetAllBarImages());

                    // Menus
                    var typemenu = AddMenu(BarNames.TypeDefinitionBrowser.ToString());
                    var assemblymenu = AddMenu(BarNames.AssemblyBrowser.ToString());
                    var assemblyrefmenu = AddMenu("???-1");
                    var modulemenu = AddMenu("???-2");
                    var methodmenu = AddMenu(BarNames.MethodDefinitionBrowser.ToString());
                    var fieldmenu = AddMenu("???-3");
                    var propertymenu = AddMenu("???-4");
                    var eventmenu = AddMenu("???-5");

                    var allmenus = new UIContext[] { typemenu, assemblymenu, assemblyrefmenu, modulemenu, methodmenu, fieldmenu, propertymenu, eventmenu };
                    var membersmenus = new UIContext[] { assemblyrefmenu, typemenu, methodmenu, fieldmenu, propertymenu, eventmenu };

                    // Type declaration menu
                    items.Add(new SubMenuUIContext(typemenu, "Inject inner class", (sender, e) => Inject(EInjectType.Class), browserimages.Images[(int)EBrowserImages.PublicClass]));
                    items.Add(new SubMenuUIContext(typemenu, "Inject inner interface", (sender, e) => Inject(EInjectType.Interface), browserimages.Images[(int)EBrowserImages.PublicInterface]));
                    items.Add(new SubMenuUIContext(typemenu, "Inject inner struct", (sender, e) => Inject(EInjectType.Struct), browserimages.Images[(int)EBrowserImages.PublicStructure]));
                    items.Add(new SubMenuUIContext(typemenu, "Inject inner enum", (sender, e) => Inject(EInjectType.Enum), browserimages.Images[(int)EBrowserImages.PublicEnum]));
                    items.Add(new SubMenuUIContext(typemenu));
                    items.Add(new SubMenuUIContext(typemenu, "Inject event", (sender, e) => Inject(EInjectType.Event), browserimages.Images[(int)EBrowserImages.PublicEvent]));
                    items.Add(new SubMenuUIContext(typemenu, "Inject field", (sender, e) => Inject(EInjectType.Field), browserimages.Images[(int)EBrowserImages.PublicField]));
                    items.Add(new SubMenuUIContext(typemenu, "Inject method", (sender, e) => Inject(EInjectType.Method), browserimages.Images[(int)EBrowserImages.PublicMethod]));
                    items.Add(new SubMenuUIContext(typemenu, "Inject constructor", (sender, e) => Inject(EInjectType.Constructor), browserimages.Images[(int)EBrowserImages.PublicConstructor]));
                    items.Add(new SubMenuUIContext(typemenu, "Inject property", (sender, e) => Inject(EInjectType.Property), browserimages.Images[(int)EBrowserImages.PublicProperty]));

                    // Shared subitems for Assembly/Module
                    foreach (MenuUIContext menu in new MenuUIContext[] { assemblymenu, modulemenu })
                    {
                        items.Add(new SubMenuUIContext(menu, "Inject class", (sender, e) => Inject(EInjectType.Class), browserimages.Images[(int)EBrowserImages.PublicClass]));
                        items.Add(new SubMenuUIContext(menu, "Inject interface", (sender, e) => Inject(EInjectType.Interface), browserimages.Images[(int)EBrowserImages.PublicInterface]));
                        items.Add(new SubMenuUIContext(menu, "Inject struct", (sender, e) => Inject(EInjectType.Struct), browserimages.Images[(int)EBrowserImages.PublicStructure]));
                        items.Add(new SubMenuUIContext(menu, "Inject enum", (sender, e) => Inject(EInjectType.Enum), browserimages.Images[(int)EBrowserImages.PublicEnum]));
                        items.Add(new SubMenuUIContext(menu, "Inject assembly reference", (sender, e) => Inject(EInjectType.AssemblyReference), browserimages.Images[(int)EBrowserImages.LinkedAssembly]));
                        items.Add(new SubMenuUIContext(menu));
                        items.Add(new SubMenuUIContext(menu, "Save as...", (sender, e) => AssemblyHelper.SaveAssembly(GetCurrentAssemblyDefinition(), GetCurrentModuleOriginalLocation()), barimages.Images[(int)EBarImages.Save]));
                        items.Add(new SubMenuUIContext(menu, "Reload", ReloadAssembly, barimages.Images[(int)EBarImages.Reload]));
                        items.Add(new SubMenuUIContext(menu, "Verify", (sender, e) => AssemblyHelper.VerifyAssembly(GetCurrentAssemblyDefinition(), GetCurrentModuleOriginalLocation()), barimages.Images[(int)EBarImages.Check]));
                    }

                    // Shared subitems for renaming/deleting
                    foreach (MenuUIContext menu in membersmenus)
                    {
                        if (menu == typemenu)
                        {
                            items.Add(new SubMenuUIContext(menu));
                        }
                        items.Add(new SubMenuUIContext(menu, "Rename...", RenameMember, barimages.Images[(int)EBarImages.New]));
                        items.Add(new SubMenuUIContext(menu, "Delete", DeleteMember, barimages.Images[(int)EBarImages.Delete]));
                    }

                    items.AddRange(allmenus);
                }
            }

            // Main events
            ab.ActiveItemChanged += this.ActiveItemChanged;
            am.AssemblyLoaded += this.AssemblyLoaded;
            am.AssemblyUnloaded += this.AssemblyUnloaded;
            
			PluginFactory.GetInstance().ReloadAssemblies(Enumerable.ToList(am.Assemblies));
            reflexilwindow.HandleItem(ab.ActiveItem);
		}
		
        /// <summary>
        /// Addin unload method
        /// </summary>
		public void Unload()
		{
            // Main events
            ab.ActiveItemChanged -= this.ActiveItemChanged;
            am.AssemblyLoaded -= this.AssemblyLoaded;
            am.AssemblyUnloaded -= this.AssemblyUnloaded;

            // Menu events
            mainMenuButton.Click -= this.Button_Click;

            // Main Window
            wm.Windows.Remove(REFLEXIL_WINDOW_ID);

            // Menu
            cbm.Bars[BarNames.Toolbar].Items.Remove(mainMenuButton);
            cbm.Bars[BarNames.Toolbar].Items.Remove(mainMenuSeparator);
        }
		#endregion

    }
}

