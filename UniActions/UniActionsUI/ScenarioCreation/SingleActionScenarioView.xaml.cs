﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UniActionsCore.ScenarioCreation;

namespace UniActionsUI.ScenarioCreation
{
    /// <summary>
    /// Interaction logic for ScenarioView.xaml
    /// </summary>
    public partial class SingleActionScenarioView : EditableUserControl
    {
        public SingleActionScenarioView()
        {
            InitializeComponent();
        }

        private ActionBag _actionBag;
        public ActionBag ActionBag
        {
            get
            {
                return _actionBag;
            }
            set
            {
                _actionBag = value;

                var action = new ActionViewExtended(_actionBag)
                {
                    IgnoreChangedEvent = true, //ignore changes on initialize
                    IsInEditMode = this.IsInEditMode
                };

                action.Changed += (o, e) => RaiseChanged();

                contentScenarioHolder.Content = action;

                action.IgnoreChangedEvent = false;
            }
        }

        public void RaiseChanged()
        {
            if (Changed != null)
                Changed(this, new EventArgs());
        }

        public event Action<object, EventArgs> Changed;
    }
}
