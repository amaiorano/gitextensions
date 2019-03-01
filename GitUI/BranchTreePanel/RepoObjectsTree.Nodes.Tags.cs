﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitCommands;
using GitUI.CommandsDialogs;
using GitUI.Properties;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;
using ResourceManager;

namespace GitUI.BranchTreePanel
{
    partial class RepoObjectsTree
    {
        private class TagNode : BaseBranchNode
        {
            private readonly IGitRef _tagInfo;

            public TagNode(Tree tree, string fullPath, IGitRef tagInfo) : base(tree, fullPath)
            {
                _tagInfo = tagInfo;
            }

            internal override void OnSelected()
            {
                if (Tree.IgnoreSelectionChangedEvent)
                {
                    return;
                }

                base.OnSelected();
                SelectRevision();
            }

            internal override void OnDoubleClick()
            {
                CreateBranch();
            }

            public void CreateBranch()
            {
                UICommands.StartCreateBranchDialog(TreeViewNode.TreeView, _tagInfo.ObjectId);
            }

            public void Delete()
            {
                UICommands.StartDeleteTagDialog(TreeViewNode.TreeView, _tagInfo.Name);
            }

            protected override void ApplyStyle()
            {
                base.ApplyStyle();
                TreeViewNode.ImageKey = TreeViewNode.SelectedImageKey = nameof(Images.TagHorizontal);
            }

            public void Checkout()
            {
                using (var form = new FormCheckoutRevision(UICommands))
                {
                    form.SetRevision(FullPath);
                    form.ShowDialog(TreeViewNode.TreeView);
                }
            }
        }

        private sealed class TagTree : Tree
        {
            public TagTree(TreeNode treeNode, IGitUICommandsSource uiCommands)
                : base(treeNode, uiCommands)
            {
            }

            protected override Task PostRepositoryChangedAsync()
            {
                return ReloadNodesAsync(LoadNodesAsync);
            }

            private async Task<Nodes> LoadNodesAsync(CancellationToken token)
            {
                await TaskScheduler.Default;
                token.ThrowIfCancellationRequested();
                return FillTagTree(Module.GetTagRefs(GitModule.GetTagRefsSortOrder.ByName), token);
            }

            private Nodes FillTagTree(IEnumerable<IGitRef> tags, CancellationToken token)
            {
                var nodes = new Nodes(this);
                var pathToNodes = new Dictionary<string, BaseBranchNode>();
                foreach (var tag in tags)
                {
                    token.ThrowIfCancellationRequested();
                    var branchNode = new TagNode(this, tag.Name, tag);
                    var parent = branchNode.CreateRootNode(pathToNodes,
                        (tree, parentPath) => new BasePathNode(tree, parentPath));
                    if (parent != null)
                    {
                        nodes.AddNode(parent);
                    }
                }

                return nodes;
            }

            protected override void PostFillTreeViewNode(bool firstTime)
            {
                if (firstTime)
                {
                    TreeViewNode.Collapse();
                }

                TreeViewNode.Text = $@"{Strings.Tags} ({Nodes.Count})";
            }
        }
    }
}
