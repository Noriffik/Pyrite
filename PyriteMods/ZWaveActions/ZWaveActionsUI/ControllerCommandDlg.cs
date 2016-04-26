using System;
using System.Windows.Forms;
using OpenZWaveDotNet;

namespace OZWForm
{
    public partial class ControllerCommandDlg : Form
    {
        private static ZWManager m_manager;
        private static ControllerCommandDlg m_dlg;
        private static uint homeId;

        private static ZWControllerCommand m_op;
        private static byte m_nodeId;
        private static DialogResult result;

        public ControllerCommandDlg(ZWManager _manager, uint _homeId, ZWControllerCommand _op, byte nodeId, bool securityEnabled)
        {
            m_manager = _manager;
            homeId = _homeId;
            m_op = _op;
            m_nodeId = nodeId;
            m_dlg = this;

            InitializeComponent();

            m_manager.OnNotification += new ManagedNotificationsHandler(NotificationHandler);
            switch (m_op)
            {
                case ZWControllerCommand.RequestNodeNeighborUpdate:
                    {
                        this.Text = "���������� ������ �������� �����";
                        this.label1.Text = "���� ��������� ������ �������� �����";

                        if (!m_manager.RequestNodeNeighborUpdate(homeId, m_nodeId))
                        {
                            MyControllerStateChangedHandler(ZWControllerState.Failed);
                        }
                        break;
                    }
                case ZWControllerCommand.AddDevice:
                    {
                        this.Text = "�������� ���������� � ZWave ����";
                        this.label1.Text =
                            "������� ����������� ������ �� Z-Wave ���������� ��� ���������� ��� � ����.\n��� ������������, ���������� ������ ���� ����� � ����������� ZWave (�� ����� 2 ������).";

                        if (!m_manager.AddNode(homeId, securityEnabled))
                        {
                            MyControllerStateChangedHandler(ZWControllerState.Failed);
                        }
                        break;
                    }
                case ZWControllerCommand.CreateNewPrimary:
                    {
                        this.Text = "������� ����� �������� ����������";
                        this.label1.Text =
                            "������� ����� ���������� � ����� �������� ������.\n������� ���������� ������ ���� ����� � ������� ������������ (�� ����� 2 ������).";

                        if (!m_manager.CreateNewPrimary(homeId))
                        {
                            MyControllerStateChangedHandler(ZWControllerState.Failed);
                        }
                        break;
                    }
                case ZWControllerCommand.ReceiveConfiguration:
                    {
                        this.Text = "�������� ������������";
                        this.label1.Text =
                            "�������� ������� ������������ �� ������� ����������.\n��������� ������ ���������� � �������� 2 ������ �� �������� �����������.";

                        if (!m_manager.ReceiveConfiguration(homeId))
                        {
                            MyControllerStateChangedHandler(ZWControllerState.Failed);
                        }
                        break;
                    }
                case ZWControllerCommand.RemoveDevice:
                    {
                        this.Text = "������ ���������� �� ����";
                        this.label1.Text =
                            "������� ����������� ������ �� ���������� ZWave ��� �������� ��� �� ����.\n�� �������� ������������, ���������� ������ ���� � ������� 2-� ������ �� ����������.";

                        if (!m_manager.RemoveNode(homeId))
                        {
                            MyControllerStateChangedHandler(ZWControllerState.Failed);
                        }
                        break;
                    }
                case ZWControllerCommand.TransferPrimaryRole:
                    {
                        this.Text = "�������� ���� ��������� ����������� ������� �����������";
                        this.label1.Text =
                            "�������� ���� ��������� ����������� ������� �����������.\n\n�� �������� ������������, ���������� ������ ���� � ������� 2-� ������ �� ����������.";

                        if (!m_manager.TransferPrimaryRole(homeId))
                        {
                            MyControllerStateChangedHandler(ZWControllerState.Failed);
                        }
                        break;
                    }
                case ZWControllerCommand.HasNodeFailed:
                    {
                        this.ButtonCancel.Enabled = false;
                        this.Text = "�������� ���� �� �������������";
                        this.label1.Text = "�������� ����.\n��� ������� �� ����� ���� ��������";

                        if (!m_manager.HasNodeFailed(homeId, m_nodeId))
                        {
                            MyControllerStateChangedHandler(ZWControllerState.Failed);
                        }
                        break;
                    }
                case ZWControllerCommand.RemoveFailedNode:
                    {
                        this.ButtonCancel.Enabled = false;
                        this.Text = "�������� ������������ ����";
                        this.label1.Text =
                            "�������� ������������ ����.\n������� �� ����� ���� ��������.";

                        if (!m_manager.RemoveFailedNode(homeId, m_nodeId))
                        {
                            MyControllerStateChangedHandler(ZWControllerState.Failed);
                        }
                        break;
                    }
                case ZWControllerCommand.ReplaceFailedNode:
                    {
                        this.ButtonCancel.Enabled = false;
                        this.Text = "������ ������������ ����";
                        this.label1.Text = "������������ ������������ ����.\n������� �� ����� ���� ��������.";

                        if (!m_manager.ReplaceFailedNode(homeId, m_nodeId))
                        {
                            MyControllerStateChangedHandler(ZWControllerState.Failed);
                        }
                        break;
                    }
                case ZWControllerCommand.RequestNetworkUpdate:
                    {
                        this.ButtonCancel.Enabled = false;
                        this.Text = "������ ���������� ���� ���������";
                        this.label1.Text = "������ ���������� ���� ���������.";

                        if (!m_manager.RequestNetworkUpdate(homeId, m_nodeId))
                        {
                            MyControllerStateChangedHandler(ZWControllerState.Failed);
                        }
                        break;
                    }
                default:
                    {
                        m_manager.OnNotification -= NotificationHandler;
                        break;
                    }
            }
        }

        public static void NotificationHandler(ZWNotification notification)
        {
            switch (notification.GetType())
            {
                case ZWNotification.Type.ControllerCommand:
                    {
                        MyControllerStateChangedHandler(((ZWControllerState)notification.GetEvent()));
                        break;
                    }
            }
        }

        public static void MyControllerStateChangedHandler(ZWControllerState state)
        {
            bool complete = false;
            String dlgText = "";
            bool buttonEnabled = true;

            switch (state)
            {
                case ZWControllerState.Waiting:
                    {
                        if (m_op == ZWControllerCommand.ReplaceFailedNode)
                        {
                            dlgText =
                                "������� ���������� ������ �� ���� ��� ������.\n�� �������� ������������, ���������� ������ ���� � ������� 2-� ������ �� ����������.";
                        }
                        break;
                    }
                case ZWControllerState.InProgress:
                    {
                        dlgText = "���������...";
                        buttonEnabled = false;
                        break;
                    }
                case ZWControllerState.Completed:
                    {
                        dlgText = "������� ���������.";
                        complete = true;
                        result = DialogResult.OK;
                        break;
                    }
                case ZWControllerState.Failed:
                    {
                        dlgText = "�� ������� ��������� �������.";
                        complete = true;
                        result = DialogResult.Abort;
                        break;
                    }
                case ZWControllerState.NodeOK:
                    {
                        dlgText = "���� ��������.";
                        complete = true;
                        result = DialogResult.No;
                        break;
                    }
                case ZWControllerState.NodeFailed:
                    {
                        dlgText = "���� ����������.";
                        complete = true;
                        result = DialogResult.Yes;
                        break;
                    }
                case ZWControllerState.Cancel:
                    {
                        dlgText = "������� ���� ��������.";
                        complete = true;
                        result = DialogResult.Cancel;
                        break;
                    }
                case ZWControllerState.Error:
                    {
                        dlgText = "������ �� ����� ���������� ������� �����������.";
                        complete = true;
                        result = DialogResult.Cancel;
                        break;
                    }
            }

            if (dlgText != "")
            {
                m_dlg.SetDialogText(dlgText);
            }

            m_dlg.SetButtonEnabled(buttonEnabled);

            if (complete)
            {
                m_dlg.SetButtonText("OK");

                m_manager.OnNotification -= NotificationHandler;
            }
        }

        private void SetDialogText(String text)
        {
            if (m_dlg.InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate () { SetDialogText(text); }));
            }
            else
            {
                m_dlg.label1.Text = text;
            }
        }

        private void SetButtonText(String text)
        {
            if (m_dlg.InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate () { SetButtonText(text); }));
            }
            else
            {
                m_dlg.ButtonCancel.Text = text;
            }
        }

        private void SetButtonEnabled(bool enabled)
        {
            if (m_dlg.InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate () { SetButtonEnabled(enabled); }));
            }
            else
            {
                m_dlg.ButtonCancel.Enabled = enabled;
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            if (ButtonCancel.Text != "OK")
            {
                m_manager.OnNotification -= NotificationHandler;

                m_manager.CancelControllerCommand(homeId);
            }

            Close();
            m_dlg.DialogResult = result;
        }
    }
}