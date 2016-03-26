﻿using System.Windows;

namespace UniActionsUI.ScenarioCreation
{
    public static class Utils
    {
        public static bool IsUserSureToDeleteCurrentOperator()
        {
            return MessageBox.Show("Вы уверены, что хотите удалить оператор со всем содержимым?", "Удаление оператора", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        }
    }
}
