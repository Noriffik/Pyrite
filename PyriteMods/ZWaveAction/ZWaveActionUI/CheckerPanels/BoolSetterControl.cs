﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZWaveActionUI.CheckerPanels
{
    public partial class BoolSetterControl : UserControl, ISetterControl
    {
        private SetterImpl _setterImpl;
        public SetterImpl Setter
        {
            get
            {
                return _setterImpl;
            }
        }
        public BoolSetterControl()
        {
            InitializeComponent();
            cbValue.SelectedIndexChanged += (o, e) => _setterImpl.Value = cbValue.SelectedIndex == 0 ? true : false;
            _setterImpl = new SetterImpl();
            _setterImpl.ValueChanged += () =>
            {
                if (_setterImpl.Value != null)
                    cbValue.SelectedIndex = (bool)_setterImpl.Value ? 0 : 1;
            };
            _setterImpl.Value = true;
        }
    }
}
