﻿// 
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2011 by DotNetNuke Corp. 
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//  documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
//  the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
//  to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in all copies or substantial portions 
//  of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
//  TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//  THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
//  CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
//  DEALINGS IN THE SOFTWARE.
// 
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Xml;
using DotNetNuke.Data;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Modules.Xml.Providers.XmlDataProvider;
using DotNetNuke.Modules.Xml.Providers.XmlRenderingProvider;
using DotNetNuke.Services.Search;

namespace DotNetNuke.Modules.Xml.Components
{
    public class XmlBaseController : BaseController
    {
        #region Constructors

        public XmlBaseController(ModuleInfo moduleConfiguration, Page page)
        {
            Initialise(moduleConfiguration, page);
        }

        public XmlBaseController(ModuleInfo moduleConfiguration) : this(moduleConfiguration, null)
        {
        }

        public XmlBaseController(PortalModuleBase parentModule) : this(parentModule.ModuleContext.Configuration, parentModule.Control.Page)
        {
            var xmlRenderingProvider = XmlRenderingProvider;
            if (xmlRenderingProvider != null && xmlRenderingProvider is IRequiresParentModule) ((IRequiresParentModule) xmlRenderingProvider).Setup(parentModule);
        }

        #endregion

        #region Properties

        /// <summary>
        ///   Accesses the Xml Source as XmlReader
        /// </summary>
        /// <remarks>
        ///   The xml source is retrieved from a file or an extern resource.
        ///   If it fails, a default &lt;noData&gt; is returned.
        /// </remarks>
        protected XmlReader XmlSource
        {
            get
            {
                var providerName = Settings[Setting.SourceProvider].DefaultIfNullOrEmpty();
                return providerName != string.Empty ? XmlDataProvider.Instance(providerName).Load(ModuleId, PortalId, Settings) : null;
            }
        }

        protected XmlRenderingProvider XmlRenderingProvider
        {
            get
            {
                var providerName = Settings[Setting.RenderingProvider].DefaultIfNullOrEmpty();
                return providerName != string.Empty ? XmlRenderingProvider.Instance(providerName) : null;
            }
        }

        public string ContentType
        {
            get { return Settings[Setting.ContentType].DefaultIfNullOrEmpty("xml|text/xml").Split("|".ToCharArray())[1]; }
        }

        /// <summary>
        ///   Returns a default filename for a separate download
        /// </summary>
        public string FileName
        {
            get { return "Result." + Settings[Setting.ContentType].DefaultIfNullOrEmpty("xml|text/xml").Split("|".ToCharArray())[0]; }
        }

        #endregion

        #region Rendering

        /// <summary>
        ///   Returns the result of the XSL Transformation into a TextWriter.
        /// </summary>
        public void Render(TextWriter output)
        {
            var renderingProvider = XmlRenderingProvider;
            using (var xml = XmlSource)
            {
                if ((renderingProvider != null) & (xml != null))
                {
                    renderingProvider.Render(xml, output, Page, ModuleConfiguration);
                }
            }
        }

        /// <summary>
        ///   Returns the result of the XSL transformation into a Stream
        /// </summary>
        public void Render(Stream stream)
        {
            using (var streamWriter = new StreamWriter(stream))
            {
                Render(streamWriter);
            }
        }

        #endregion

        #region Other Methods

        /// <summary>
        ///   Clears the indexed content of the DotNetNuke search for this module
        /// </summary>
        public void ClearSearchIndex()
        {
            // FIX: for Error CS1061  'DataProvider' does not contain a definition for 'DeleteSearchItems' and no extension method 'DeleteSearchItems' accepting a first argument of type 'DataProvider' could be found(are you missing a using directive or an assembly reference ?)	Xml D:\Materijali\SD\Portal2015\Dev\Prototips\DNN.XML\Components\XmlBaseController.cs   132 Active
            //DataProvider.Instance().DeleteSearchItems(ModuleId);
            var moduleSearchItems = SearchDataStoreController.GetSearchItems(ModuleId);
            foreach (var searchItem in moduleSearchItems)
            {
                SearchDataStoreController.DeleteSearchItem(searchItem.Value.SearchItemId);
            }
        }


        /// <summary>
        ///   Returns the way how the module is rendered (<see cref = "ShowMode"></see>).
        /// </summary>
        public ShowMode CheckShowMode(string mode)
        {
            var enableParam = Settings[Setting.EnableParam].DefaultIfNullOrEmpty();
            var enableValue = Settings[Setting.EnableValue].DefaultIfNullOrEmpty();
            if (enableParam.Length > 0 && enableValue.Length > 0 && HttpContext.Current.Request.QueryString[enableParam].DefaultIfNullOrEmpty() != enableValue)
                return ShowMode.Disabled;
            if (enableParam.Length > 0 && enableValue.Length == 0 && HttpContext.Current.Request.QueryString[enableParam].DefaultIfNullOrEmpty().Length > 0)
                return ShowMode.Disabled;

            mode = mode.DefaultIfNullOrEmpty(Settings[Setting.RenderTo].DefaultIfNullOrEmpty(Enum.GetName(typeof (ShowMode), ShowMode.Inline)));
            try
            {
                return (ShowMode) Enum.Parse(typeof (ShowMode), mode);
            }
            catch
            {
                return ShowMode.Inline;
            }
        }

        #endregion
    }
}