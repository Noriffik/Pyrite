﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using UniActionsClientIntefaces;
using UniActionsCore.ScenarioCreation;

namespace UniActionsUI.ScenarioCreation
{
    public class ActionViewContext : DependencyObject
    {
        public static readonly DependencyProperty ActionStringProperty;
        public static readonly DependencyProperty ParamsVisibilityProperty;

        static ActionViewContext()
        {
            ActionStringProperty = DependencyProperty.Register("ActionString", typeof(string), typeof(ActionViewContext));
            ParamsVisibilityProperty = DependencyProperty.Register("ParamsVisibility", typeof(Visibility), typeof(ActionViewContext));
        }

        public ActionViewContext(ActionBag actionBag)
        {
            this._actionBag = actionBag;
            this.ParamsVisibility = this._actionBag.Action.AllowUserSettings ? Visibility.Visible : Visibility.Collapsed;
            ProcessActionBag();
        }

        public ActionViewContext() : this(null) { }

        public string ActionString
        {
            get
            {
                return (string)GetValue(ActionStringProperty);
            }
            set
            {
                SetValue(ActionStringProperty, value);
            }
        }

        public Visibility ParamsVisibility
        {
            get
            {
                return (Visibility)GetValue(ParamsVisibilityProperty);
            }
            set
            {
                SetValue(ParamsVisibilityProperty, value);
            }
        }

        public IEnumerable<ActionNamePair> AllCustomActions
        {
            get
            {
                return App.Uni.ModulesControl.CustomActions.Select(x =>
                    new ActionNamePair(App.Uni.ModulesControl.GetViewName(x).Value, x)
                ).OrderBy(x => x.Name);
            }
        }

        public ActionNamePair ActionNamePair
        {
            get
            {
                ProcessActionBag();
                return new ActionNamePair(_actionBag.Action.Name, _actionBag.Action.GetType());
            }
            set
            {
                _actionBag.Action = (ICustomAction)value.ActionType.GetConstructor(new Type[0]).Invoke(new object[0]);
                this.ParamsVisibility = this._actionBag.Action.AllowUserSettings ? Visibility.Visible : Visibility.Collapsed;
                BeginActionUserSettings();
                RaiseChanged();
            }
        }

        public void ExecuteCurrentAction()
        {
            ProcessActionBag();
            Helper.SafeExecute(_actionBag.Action);
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
                ProcessActionBag();
                _actionBag.Action = value.Action;
                ProcessActionString();
                this.ParamsVisibility = this._actionBag.Action.AllowUserSettings ? Visibility.Visible : Visibility.Collapsed;
                RaiseChanged();
            }
        }

        public void ProcessActionBag()
        {
            if (_actionBag == null)
                _actionBag = new ActionBag();
            if (_actionBag.Action == null)
                _actionBag.Action = new DoNothingAction();
            ProcessActionString();
        }

        public string ActionStringSplitter = ";";

        public void ProcessActionString()
        {
            var actionString = Helper.CreateParamsViewString(_actionBag.Action).Replace(";", ActionStringSplitter);
            if (!string.IsNullOrWhiteSpace(actionString))
                this.ActionString = "(" + actionString + ")";
            else
                this.ActionString = actionString;
        }

        public void BeginActionUserSettings()
        {
            ProcessActionBag();
            if (_actionBag.Action.AllowUserSettings && _actionBag.Action.BeginUserSettings())
            {
                ProcessActionString();
                RaiseChanged();
            }
        }

        public void RaiseChanged()
        {
            if (Changed != null)
                Changed(this, new EventArgs());
        }

        public event Action<object, EventArgs> Changed;
    }

    public struct ActionNamePair
    {
        public ActionNamePair(string name, Type actionType)
        {
            Name = name;
            ActionType = actionType;
        }
        public string Name { get; private set; }
        public Type ActionType { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
