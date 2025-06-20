using CZN.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CZN.Services
{
    public class Helper
    {
        private static CZNEntities4 _context;

        public static CZNEntities4 GetContext()
        {
            if (_context == null)
            {
                _context = new CZNEntities4();
            }
            return _context;
        }
        public static Users AuthUser(string username, string password)
        {
            try
            {
                string hashedPassword = Hash.HashPassword(password);

                using (var freshContext = new CZNEntities4())
                {
                    return freshContext.Users.FirstOrDefault(u =>
                        u.Username == username &&
                        u.PasswordHash == hashedPassword);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}");
                return null;
            }
        }
        public static bool IsUserLocked(int userId)
        {
            using (var context = new CZNEntities4())
            {
                return context.Users
                    .Where(u => u.UserID == userId)
                    .Select(u => u.IsLocked)
                    .FirstOrDefault();
            }
        }
        public static bool IsAdmin(Users user)
        {
            return user != null && user.RoleID == 1 && !user.IsLocked;
        }
        public static List<AdminEmployeesModel> GetEmployeesWithDetails(int currentAdminId)
        {
            using (var context = new CZNEntities4())
            {
                return (from e in context.Employees
                        join d in context.Departments on e.DepartmentID equals d.DepartmentID
                        join dist in context.Districts on d.DistrictID equals dist.DistrictID
                        join p in context.Positions on e.PositionID equals p.PositionID
                        from c in context.ContactInfo.Where(ci => ci.EmployeeID == e.EmployeeID).DefaultIfEmpty()
                        from u in context.Users.Where(u => u.EmployeeID == e.EmployeeID).DefaultIfEmpty()
                        select new AdminEmployeesModel
                        {
                            EmployeeID = e.EmployeeID,
                            UserID = u != null ? u.UserID : 0,
                            CurrentAdminId = currentAdminId,
                            LastName = e.LastName,
                            FirstName = e.FirstName,
                            MiddleName = e.MiddleName,
                            Department = d.Name,
                            DepartmentAddress = d.Address,
                            DepartmentPhone = d.DepartmentPhone,
                            DistrictName = dist.Name,
                            Position = p.Title,
                            InternalPhone = c != null ? c.InternalPhone : null,
                            CityPhone = c != null ? c.CityPhone : null,
                            MobilePhone = c != null ? c.MobilePhone : null,
                            Email = c != null ? c.Email : null,
                            Notes = c != null ? c.Notes : null,
                            IsAdmin = u != null && u.RoleID == 1,
                            IsLocked = u != null && u.IsLocked,
                            Username = u != null ? u.Username : null
                        }).ToList();
            }
        }

        public static bool SaveEmployee(AdminEmployeesModel model, string username = null, string password = null)
        {
            try
            {
                using (var context = new CZNEntities4())
                {
                    using (var transaction = context.Database.BeginTransaction())
                    {
                        if (model.UserID != 0 && model.UserID == model.CurrentAdminId && !model.IsAdmin)
                        {
                            MessageBox.Show("Нельзя снять админские права с самого себя!");
                            return false;
                        }

                        if (model.IsAdmin && string.IsNullOrWhiteSpace(username))
                        {
                            MessageBox.Show("Укажите логин для администратора");
                            return false;
                        }

                        if (model.IsAdmin && context.Users.Any(u => u.Username == username && u.UserID != model.UserID))
                        {
                            MessageBox.Show("Пользователь с таким логином уже существует");
                            return false;
                        }

                        var employee = model.EmployeeID == 0
                            ? new Employees()
                            : context.Employees.FirstOrDefault(e => e.EmployeeID == model.EmployeeID);

                        if (employee == null) return false;

                        employee.LastName = model.LastName;
                        employee.FirstName = model.FirstName;
                        employee.MiddleName = model.MiddleName;

                        var department = context.Departments.FirstOrDefault(d => d.Name == model.Department);
                        var position = context.Positions.FirstOrDefault(p => p.Title == model.Position);

                        if (department == null || position == null)
                        {
                            MessageBox.Show("Указанный отдел или должность не найдены");
                            return false;
                        }

                        employee.DepartmentID = department.DepartmentID;
                        employee.PositionID = position.PositionID;

                        if (model.EmployeeID == 0)
                        {
                            context.Employees.Add(employee);
                        }
                        context.SaveChanges();

                        var contact = context.ContactInfo.FirstOrDefault(c => c.EmployeeID == employee.EmployeeID)
                                    ?? new ContactInfo { EmployeeID = employee.EmployeeID };

                        contact.InternalPhone = model.InternalPhone;
                        contact.CityPhone = model.CityPhone;
                        contact.MobilePhone = model.MobilePhone;
                        contact.Email = model.Email;
                        contact.Notes = model.Notes;

                        if (contact.ContactID == 0)
                        {
                            context.ContactInfo.Add(contact);
                        }
                        context.SaveChanges();

                        var user = context.Users.FirstOrDefault(u => u.EmployeeID == employee.EmployeeID);

                        if (model.IsAdmin)
                        {
                            if (user == null)
                            {
                                user = new Users
                                {
                                    EmployeeID = employee.EmployeeID,
                                    RoleID = 1,
                                    IsLocked = model.IsLocked,
                                    Username = username
                                };
                                context.Users.Add(user);
                            }

                            user.RoleID = 1;
                            user.Username = username;
                            user.IsLocked = model.IsLocked;

                            if (!string.IsNullOrEmpty(password))
                            {
                                user.PasswordHash = Hash.HashPassword(password);
                            }
                        }
                        else
                        {
                            if (user == null)
                            {
                                user = new Users
                                {
                                    EmployeeID = employee.EmployeeID,
                                    RoleID = 2,
                                    IsLocked = model.IsLocked
                                };
                                context.Users.Add(user);
                            }
                            else
                            {
                                user.RoleID = 2;
                                user.Username = null;
                                user.PasswordHash = null;
                                user.IsLocked = model.IsLocked;
                            }
                        }

                        context.SaveChanges();
                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения: {ex.Message}");
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
                return false;
            }
        }

        public static bool DeleteEmployee(int employeeId)
        {
            try
            {
                var context = GetContext();
                using (var transaction = context.Database.BeginTransaction())
                {
                    var contacts = context.ContactInfo.Where(c => c.EmployeeID == employeeId).ToList();
                    context.ContactInfo.RemoveRange(contacts);

                    var users = context.Users.Where(u => u.EmployeeID == employeeId).ToList();
                    context.Users.RemoveRange(users);


                    var employee = context.Employees.FirstOrDefault(e => e.EmployeeID == employeeId);
                    if (employee != null)
                    {
                        context.Employees.Remove(employee);
                    }

                    context.SaveChanges();
                    transaction.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка удаления сотрудника: {ex.Message}");
                return false;
            }
        }
    }
}
