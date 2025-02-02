// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Axe.Windows.Core.Bases;
using Axe.Windows.Core.Types;
using Axe.Windows.Telemetry;
using Axe.Windows.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UIAutomationClient;

namespace Axe.Windows.Desktop.UIAutomation
{
    /// <summary>
    /// Wrapper for DesktopElement
    /// </summary>
    public class DesktopElement : A11yElement
    {
        private static readonly List<int> _excludedPropertyIds = new List<int>()
        {
            // do not remove the following since it causes an issue with Edge when this value is there.
            // The clickable point is retrieved later once it can be determined the element does not belong to Edge
            PropertyType.UIA_ClickablePointPropertyId,
        };

        /// <summary>
        /// Constructor for DesktopElement
        /// when keepElement is true, it is for live action. otherwise, it is for snapshot.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="keepElement">default is true</param>
        /// <param name="setMembers">default is true. if it is true, sets propeties and patterns at construction</param>
        public DesktopElement(IUIAutomationElement element, bool keepElement = true, bool setMembers = true)
        {
            this.PlatformObject = element;

            if (setMembers)
            {
                this.PopulateAllPropertiesWithLiveData();
            }

            if (keepElement == false)
            {
                this.PlatformObject = null;
            }

            this.Children = new List<A11yElement>();
        }

        /// <summary>
        /// check whether property is excluded from retrieved.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsExcludedProperty(int id, string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            return id == 0
                   || name.EndsWith("PatternAvailable", System.StringComparison.Ordinal)
                   || name.EndsWith("Pattern2Available", System.StringComparison.Ordinal)
                   || _excludedPropertyIds.Contains(id);
        }

        public override string ToString()
        {
            return this.Glimpse;
        }

        #region Static methods for validation
        static readonly int CurrentPId = System.Diagnostics.Process.GetCurrentProcess().Id;

        /// <summary>
        /// Check whether IUIAutomation is from current process or not.
        /// </summary>
        /// <param name="uia"></param>
        /// <returns></returns>
        public static bool IsFromCurrentProcess(IUIAutomationElement uia)
        {
            var pId = GetPropertyValue(uia, PropertyType.UIA_ProcessIdPropertyId);

            bool isCP = false;

            if (pId != null)
            {
                isCP = pId == CurrentPId;

#pragma warning disable CA1806 // Do not ignore method results
                NativeMethods.VariantClear(ref pId);
#pragma warning restore CA1806 // Do not ignore method results
            }

            return isCP;
        }

        /// <summary>
        /// Get Property value safely
        /// </summary>
        /// <param name="element"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private static dynamic GetPropertyValue(IUIAutomationElement element, int id)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            dynamic value = null;
            try
            {
                value = element.GetCurrentPropertyValue(id);
                if (id == PropertyType.UIA_LabeledByPropertyId && value != null)
                {
                    value = GetHeaderOfLabelBy(value);
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            {
                e.ReportException();
                value = null;
            }
#pragma warning restore CA1031 // Do not catch general exception types

            return value;
        }

        private static string GetHeaderOfLabelBy(IUIAutomationElement e)
        {
            return string.Format(CultureInfo.InvariantCulture, "{1} \"{0}\"", GetPropertyValue(e, PropertyType.UIA_NamePropertyId), GetPropertyValue(e, PropertyType.UIA_LocalizedControlTypePropertyId));
        }
        #endregion

        /// <summary>
        /// Dispos for Desktop Level
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (this.PlatformObject != null)
                {
                    // to make sure for release.
                    Marshal.ReleaseComObject(this.PlatformObject);
                    this.PlatformObject = null;
                }
                base.Dispose(disposing);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            {
                e.ReportException();
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }
}
