﻿using HierarchicalData;
using System;
using System.Xml.Serialization;
using UniActionsClientIntefaces;

namespace UniStandartActions.Actions
{
    [Serializable]
    public class PowerOffAction : ICustomAction
    {
        [XmlIgnore]
        public bool AllowUserSettings { get { return true; } }

        public int Timeout { get;set; }
        public bool CanCancel { get; set; }
        public bool Restart { get; set; }

        public string StateOff = "Выключить компьютер";
        public string StateRestart = "Перезагрузить компьютер";

        public string Do(string inputState)
        {
            IsBusyNow = true;
            var form = new PowerOffForm() { 
                Timer = Timeout, 
                CanCancel = CanCancel, 
                Restart = Restart 
            };

            form.Show();
            form.Start();
            IsBusyNow = false;

            return StateOff;
        }

        [XmlIgnore]
        public string State
        {
            get
            {
                return Restart ? StateRestart : StateOff;
            }
        }

        [XmlIgnore]
        public string Name
        {
            get { return State; }
        }

        public bool BeginUserSettings()
        {
            var form = new PowerOffActionView();
            form.Timer = this.Timeout;
            form.CanCancel = this.CanCancel;
            form.Restart = this.Restart;
            if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.Timeout = form.Timer;
                this.CanCancel = form.CanCancel;
                this.Restart = form.Restart;
                return true;
            }
            return false;
        }

        [XmlIgnore]        
        public bool IsBusyNow
        {
            get;
            private set;
        }
        public void Refresh() { }
    }
}
