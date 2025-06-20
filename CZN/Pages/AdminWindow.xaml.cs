using CZN.Models;
using CZN.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CZN.Pages
{
    /// <summary>
    /// Логика взаимодействия для AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        private int _currentAdminId;
        private List<AdminEmployeesModel> _allEmployees;

        public AdminWindow(int currentAdminId)
        {
            if (Helper.IsUserLocked(currentAdminId))
            {
                MessageBox.Show("Ваш доступ ограничен! Вход в панель администратора невозможен.",
                    "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Error);

                new UserWindow(currentAdminId).Show();
                this.Close();
                return;
            }

            InitializeComponent();
            _currentAdminId = currentAdminId;
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                _allEmployees = Helper.GetEmployeesWithDetails(_currentAdminId);
                dgEmployees.ItemsSource = _allEmployees;
                dgEmployees.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = txtSearch.Text.ToLower();
            dgEmployees.ItemsSource = _allEmployees.Where(emp =>
                (emp.LastName?.ToLower() ?? "").Contains(searchText) ||
                (emp.FirstName?.ToLower() ?? "").Contains(searchText) ||
                (emp.MiddleName?.ToLower() ?? "").Contains(searchText) ||
                (emp.Department?.ToLower() ?? "").Contains(searchText) ||
                (emp.Position?.ToLower() ?? "").Contains(searchText)
            );
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployees();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EmployeeEditWindow();
            if (editWindow.ShowDialog() == true)
            {
                LoadEmployees();
                txtSearch.Clear();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var employee = (sender as Button).DataContext as AdminEmployeesModel;
            employee.CurrentAdminId = _currentAdminId;
            var editWindow = new EmployeeEditWindow(employee);
            if (editWindow.ShowDialog() == true)
            {
                LoadEmployees();
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var employee = (sender as Button).DataContext as AdminEmployeesModel;

            if (employee.UserID == _currentAdminId)
            {
                MessageBox.Show("Вы не можете удалить себя!");
                return;
            }

            if (MessageBox.Show($"Вы точно хотите удалить сотрудника {employee.LastName}?",
                               "Подтверждение удаления",
                               MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (Helper.DeleteEmployee(employee.EmployeeID))
                {
                    MessageBox.Show("Сотрудник успешно удален");
                    LoadEmployees();
                }
                else
                {
                    MessageBox.Show("Не удалось удалить сотрудника", "Ошибка");
                }
            }
        }
        private void LockCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.DataContext is AdminEmployeesModel employee)
            {
                bool newLockedState = checkBox.IsChecked == true;

                if (employee.UserID == _currentAdminId)
                {
                    MessageBox.Show("Вы не можете заблокировать себя!");
                    checkBox.IsChecked = false;
                    return;
                }

                try
                {
                    using (var context = new CZNEntities4())
                    {
                        var user = context.Users.FirstOrDefault(u => u.EmployeeID == employee.EmployeeID);
                        if (user != null)
                        {
                            user.IsLocked = newLockedState;
                            context.SaveChanges();

                            _allEmployees = Helper.GetEmployeesWithDetails(_currentAdminId);
                            dgEmployees.ItemsSource = _allEmployees;
                            dgEmployees.Items.Refresh();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка изменения блокировки: {ex.Message}");
                    checkBox.IsChecked = !newLockedState;
                }
            }
        }
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.Properties.Contains("CurrentUserId"))
            {
                Application.Current.Properties.Remove("CurrentUserId");
            }

            var loginWindow = new LoginWindow();
            loginWindow.Show();

            this.Close();
        }
    }
}
