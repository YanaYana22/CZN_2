using CZN.Models;
using CZN.Services;
using System;
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
using System.Windows.Shapes;

namespace CZN.Pages
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private int currentUserId;
        public LoginWindow()
        {
            InitializeComponent();
        }
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Password;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Введите логин и пароль");
                    return;
                }

                var user = Helper.AuthUser(username, password);

                if (user == null)
                {
                    MessageBox.Show("Неверный логин или пароль");
                    return;
                }

                bool isLocked;
                using (var freshContext = new CZNEntities4())
                {
                    isLocked = freshContext.Users
                        .Where(u => u.UserID == user.UserID)
                        .Select(u => u.IsLocked)
                        .FirstOrDefault();
                }

                if (isLocked)
                {
                    string message = user.RoleID == 1
                        ? "Ваш доступ как администратора ограничен!"
                        : "Ваша учетная запись заблокирована!";

                    MessageBox.Show(message, "Ограничение доступа",
                        MessageBoxButton.OK, MessageBoxImage.Warning);

                    new UserWindow(user.UserID).Show();
                    this.Close();
                    return;
                }

                Window window;
                if (Helper.IsAdmin(user))
                {
                    window = new AdminWindow(user.UserID);
                }
                else
                {
                    window = new UserWindow(user.UserID);
                }

                window.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void linkGuest_Click(object sender, RoutedEventArgs e)
        {
            UserWindow userWindow = new UserWindow(0);
            userWindow.Show();
            this.Close();
        }
    }
}
