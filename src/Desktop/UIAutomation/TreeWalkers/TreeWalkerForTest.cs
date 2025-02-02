// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Axe.Windows.Core.Bases;
using Axe.Windows.Core.Enums;
using Axe.Windows.Core.Misc;
using Axe.Windows.RuleSelection;
using Axe.Windows.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UIAutomationClient;

namespace Axe.Windows.Desktop.UIAutomation.TreeWalkers
{
    internal interface ITreeWalkerForTest
    {
        IList<A11yElement> Elements { get; }
        A11yElement TopMostElement { get; }
        void RefreshTreeData(TreeViewMode mode);
    }

    /// <summary>
    /// Wrapper for UIAutomation Tree Walker 2nd edition
    /// Do tree walking by retrieving all children at once.
    /// </summary>
    internal class TreeWalkerForTest : ITreeWalkerForTest
    {
        private readonly BoundedCounter _elementCounter;

        /// <summary>
        /// List of all Elements including SelectedElement and descendents
        /// </summary>
        public IList<A11yElement> Elements { get; }

        public TimeSpan LastWalkTime { get; private set; }

        public TreeViewMode WalkerMode { get; private set; }

        public A11yElement SelectedElement { get; private set; }

        public A11yElement TopMostElement { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="element"></param>
        /// <param name="elementCounter">Provides an upper bound on the number of elements we'll allow to be tested</param>
        public TreeWalkerForTest(A11yElement element, BoundedCounter elementCounter)
        {
            _elementCounter = elementCounter;
            this.SelectedElement = element;
            this.Elements = new List<A11yElement>();
        }

        /// <summary>
        /// Refresh tree node data with all children at once.
        /// <param name="mode">indicate the mode</param>
        /// </summary>
        public void RefreshTreeData(TreeViewMode mode)
        {
            this.WalkerMode = mode;
            if (this.Elements.Count != 0)
            {
                this.Elements.Clear();
            }

            //Set parent of Root explicitly for testing.
            var ancestry = new DesktopElementAncestry(this.WalkerMode, this.SelectedElement);

            // Pre-count the ancestors in our bounded count, so that our count is accurate
            _elementCounter.TryAdd(ancestry.Items.Count);

            this.TopMostElement = ancestry.First;

            // clear children
            ListHelper.DisposeAllItemsAndClearList(this.SelectedElement.Children);
            this.SelectedElement.UniqueId = 0;

            PopulateChildrenTreeNode(this.SelectedElement, ancestry.Last, ancestry.NextId);

            // do population of ancesters all togather with children
            var list = new List<A11yElement>(this.Elements);
            foreach (var item in ancestry.Items)
            {
                this.Elements.Add(item);
            }

            // populate Elements first
            this.Elements.AsParallel().ForAll(e => e.PopulateAllPropertiesWithLiveData());

            // check whether there is any elements which couldn't be updated in parallel, if so, update it in sequence.
            var nuel = this.Elements.Where(e => e.Properties == null);

            if (nuel.Any())
            {
                nuel.ToList().ForEach(e => e.PopulateAllPropertiesWithLiveData());
            }

            // run tests
            list.AsParallel().ForAll(e =>
            {
                e.ScanResults?.Items.Clear();
                RuleRunner.Run(e);
            });
        }

        /// <summary>
        /// Populate tree by retrieving all children at once.
        /// </summary>
        /// <param name="rootNode"></param>
        /// <param name="parentNode"></param>
        /// <param name="startChildId"></param>
        private int PopulateChildrenTreeNode(A11yElement rootNode, A11yElement parentNode, int startChildId)
        {
            this.Elements.Add(rootNode);

            rootNode.Parent = parentNode;
            rootNode.TreeWalkerMode = this.WalkerMode; // set tree walker mode.

            IUIAutomationTreeWalker walker = A11yAutomation.GetTreeWalker(this.WalkerMode);
            IUIAutomationElement child = (IUIAutomationElement)rootNode.PlatformObject;

            if (child != null)
            {
                try
                {
                    child = walker.GetFirstChildElement(child);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
                {
                    ex.ReportException();
                    child = null;
                    System.Diagnostics.Trace.WriteLine("Tree walker exception: " + ex);
                }
#pragma warning restore CA1031 // Do not catch general exception types

                while (child != null && _elementCounter.TryIncrement())
                {
#pragma warning disable CA2000 // childNode will be disposed by the parent node
                    // Create child without populating basic property. it will be set all at once in parallel.
                    var childNode = new DesktopElement(child, true, false);
#pragma warning restore CA2000 // childNode will be disposed by the parent node

                    rootNode.Children.Add(childNode);
                    childNode.Parent = rootNode;
                    childNode.UniqueId = startChildId++;
                    startChildId = PopulateChildrenTreeNode(childNode, rootNode, startChildId);
                    try
                    {
                        child = walker.GetNextSiblingElement(child);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
                    {
                        ex.ReportException();
                        child = null;
                        System.Diagnostics.Trace.WriteLine("Tree walker exception: " + ex);
                    }
#pragma warning restore CA1031 // Do not catch general exception types
                }
            }

            Marshal.ReleaseComObject(walker);

            return startChildId;
        }
    }
}
