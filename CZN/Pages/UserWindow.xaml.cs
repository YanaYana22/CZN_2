using CZN.Models;
using CZN.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;
namespace CZN.Pages

{

    public partial class UserWindow : Window
    {
        private int _currentUserId;
        private List<AdminEmployeesModel> _allEmployees;

        public UserWindow(int currentUserId)
        {
            InitializeComponent();
            _currentUserId = currentUserId;
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                _allEmployees = Helper.GetEmployeesWithDetails(_currentUserId);
                InitializeOffices();
                _allEmployees = Helper.GetEmployeesWithDetails(_currentUserId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeOffices()
        {
            tabOffices.Items.Clear();

            var offices = _allEmployees
                .GroupBy(e => new { e.Department, e.DepartmentAddress, e.DepartmentPhone })
                .OrderBy(g => g.Key.Department);

            foreach (var office in offices)
            {
                var itemsControl = new ItemsControl
                {
                    ItemTemplate = (DataTemplate)Resources["EmployeeTemplate"],
                    ItemsSource = office
                        .OrderBy(e => e.LastName)
                        .ThenBy(e => e.FirstName)
                        .Select(e => new EmployeeViewModel(e))
                };

                var headerText = $"{office.Key.Department}\n{office.Key.DepartmentAddress}";
                if (!string.IsNullOrEmpty(office.Key.DepartmentPhone))
                {
                    headerText += $"\nТел: {office.Key.DepartmentPhone}";
                }

                var tabItem = new TabItem
                {
                    Header = headerText,
                    Content = itemsControl
                };

                tabOffices.Items.Add(tabItem);
            }
        }
        private void EmployeesScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!(sender is ScrollViewer scv)) return;

            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
        private void UpdateEmployeesList()
        {
            var searchText = txtSearch.Text?.ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                InitializeOffices();
                return;
            }

            var foundEmployees = new List<AdminEmployeesModel>();

            foreach (var emp in _allEmployees)
            {
                if ((emp.LastName?.ToLower().Contains(searchText) ?? false) ||
                    (emp.InternalPhone?.Contains(searchText) ?? false) ||
                    (emp.Position?.ToLower().Contains(searchText) ?? false))
                {
                    foundEmployees.Add(emp);
                }
            }

            var groupedResults = foundEmployees
                .GroupBy(e => new { e.Department, e.DepartmentAddress, e.DepartmentPhone })
                .OrderBy(g => g.Key.Department);

            tabOffices.Items.Clear();

            foreach (var group in groupedResults)
            {
                var headerText = $"{group.Key.Department}\n{group.Key.DepartmentAddress}";
                if (!string.IsNullOrEmpty(group.Key.DepartmentPhone))
                {
                    headerText += $"\nТел: {group.Key.DepartmentPhone}";
                }

                var tabItem = new TabItem
                {
                    Header = headerText,
                    Content = new ScrollViewer
                    {
                        Content = new ItemsControl
                        {
                            ItemTemplate = (DataTemplate)Resources["EmployeeTemplate"],
                            ItemsSource = group
                                .OrderBy(e => e.LastName)
                                .ThenBy(e => e.FirstName)
                                .Select(e => new EmployeeViewModel(e))
                        }
                    },
                    Background = new SolidColorBrush(Color.FromArgb(50, 0, 122, 204))
                };

                tabOffices.Items.Add(tabItem);
            }

            if (foundEmployees.Count == 0)
            {
                var noResultsTab = new TabItem
                {
                    Header = "Результаты поиска",
                    Content = new TextBlock
                    {
                        Text = "Сотрудники не найдены",
                        Foreground = Brushes.White,
                        FontSize = 21,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };

                tabOffices.Items.Add(noResultsTab);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateEmployeesList();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployees();
            txtSearch.Clear();
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

    public class EmployeeViewModel
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string FullName => $"{LastName} {FirstName} {MiddleName}";
        public string Position { get; set; }
        public string Department { get; set; }
        public string DepartmentAddress { get; set; }
        public string InternalPhone { get; set; }
        public string Email { get; set; }
        public string CityPhone { get; set; }
        public string MobilePhone { get; set; }
        public bool IsLocked { get; set; }

        public EmployeeViewModel(AdminEmployeesModel employee)
        {
            LastName = employee.LastName;
            FirstName = employee.FirstName;
            MiddleName = employee.MiddleName;
            Position = employee.Position;
            Department = employee.Department;
            DepartmentAddress = employee.DepartmentAddress;
            InternalPhone = employee.InternalPhone;
            Email = employee.Email;
            CityPhone = employee.CityPhone;
            MobilePhone = employee.MobilePhone;
            IsLocked = employee.IsLocked;
        }
    }
}