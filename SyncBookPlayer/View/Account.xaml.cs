namespace SyncBookPlayer.View;

using ATL.Logging;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public partial class Account : ContentPage
{
	public Account()
	{
		InitializeComponent();
        CheckLogin();
    }

    private void NewAccount_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
		if (NewAccount.IsChecked)
			LoginBut.Text = "Создать аккаунт";
		else
            LoginBut.Text = "Войти";
    }

    private async void LoginBut_Clicked(object sender, EventArgs e)
    {
        LoginBut.IsEnabled = false;
        Errors.Text = "";
        bool allowlogin = true;
        string login = Login.Text == null ? "" : Login.Text.ToString().ToLower();
        string password = Pass1.Text == null ? "" : Pass1.Text.ToString();
        string password2 = Pass2.Text == null ? "" : Pass2.Text.ToString();
        if (!IsValidInput(login))
        {
            allowlogin = false;
            Errors.Text += "Некорректное имя пользователя\n";
        }
        if (!IsValidInput(password))
        {
            allowlogin = false;
            Errors.Text += "Некорректный пароль\n";
        }
        if (!NewAccount.IsChecked)
        {
            if (allowlogin) {
                try
                {
                    string connString = "Server=ep-falling-cake-416088.eu-central-1.aws.neon.tech;Username=DAROMON;Database=neondb;Port=5432;Password=NVgYsqK8hyP6;SSLMode=Prefer;Timeout=10";
                    var conn = new NpgsqlConnection(connString);
                    await conn.OpenAsync();
                    using (var command = new NpgsqlCommand($"SELECT id FROM accounts WHERE login = @login AND password = @password;", conn))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", Md5(password));
                        var x = command.ExecuteScalar();
                        if (x != null) {
                            SaveAccount(x.ToString());
                            CheckLogin(x.ToString());
                        }
                        else
                        {
                            Errors.Text += "Неверный логин или пароль\n";
                        }
                    }
                    await conn.CloseAsync();
                }
                catch
                {
                    Errors.Text = "Ошибка подключения";
                }
            }
        }
        else
        {
            if (password != password2) {
                allowlogin = false;
                Errors.Text += "Пароли не совпадают\n";
            }
            if (allowlogin)
            {
                try { 
                    string connString = "Server=ep-falling-cake-416088.eu-central-1.aws.neon.tech;Username=DAROMON;Database=neondb;Port=5432;Password=NVgYsqK8hyP6;SSLMode=Prefer;Timeout=10";
                    var conn = new NpgsqlConnection(connString);
                    await conn.OpenAsync();
                    using (var command = new NpgsqlCommand($"INSERT INTO accounts (login, password) VALUES (@login, @password) RETURNING id;", conn))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", Md5(password));
                        try
                        {
                            var x = (int)command.ExecuteScalar();
                            if (x > 0)
                            {
                                SaveAccount(x.ToString());
                                CheckLogin(x.ToString());
                            }
                        }
                        catch
                        {
                            Errors.Text += "Ошибка регистрации\n";
                        }
                    }
                    await conn.CloseAsync();
                }
                catch
                {
                    Errors.Text = "Ошибка подключения";
                }
            }
            
        }
        LoginBut.IsEnabled = true;
    }
    public static string Md5(string password)
    {
        MD5 md5 = MD5.Create();
        byte[] b = Encoding.ASCII.GetBytes(password);
        byte[] hash = md5.ComputeHash(b);
        StringBuilder sb = new StringBuilder();
        foreach (var a in hash)
            sb.Append(a.ToString("X2"));
        return Convert.ToString(sb);
    }
    public bool IsValidInput(string input)
    {
        string pattern = @"^[a-zA-Z0-9_@!#\$%^&*()+=\-\[\]\';,./{}|"":<>?~]+$";
        return Regex.IsMatch(input, pattern);
    }

    public async void SaveAccount(string login)
    {
        await SecureStorage.Default.SetAsync("User", login);
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        SecureStorage.Default.Remove("User");
        CheckLogin();
    }
    async void CheckLogin(string login2 = null)
    {
        string login;
        if (login2 == null)
            login = await SecureStorage.Default.GetAsync("User");
        else
            login = login2;

        if (login == null)
        {
            notlogined.IsVisible = true;
            logined.IsVisible = false;
        }
        else
        {
            try
            {
                string connString = "Server=ep-falling-cake-416088.eu-central-1.aws.neon.tech;Username=DAROMON;Database=neondb;Port=5432;Password=NVgYsqK8hyP6;SSLMode=Prefer;Timeout=10";
                var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();
                using (var command = new NpgsqlCommand($"SELECT login FROM accounts WHERE id = {login};", conn))
                {
                    var x = command.ExecuteScalar();
                    if (x != null)
                    {
                        Hello.Text = $"Здравствуйте, {x}!";
                    }
                    else
                    {
                        Hello.Text = $"Ошибка авторизации";
                    }
                }
                await conn.CloseAsync();
            }
            catch
            {
                Hello.Text = "Проверьте подключение к интернету";
            }
            notlogined.IsVisible = false;
            logined.IsVisible = true;
        }
    }
}