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
        if (login == "acccounts" || !IsValidInput(login))
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
                long result;
                string connString = "Server=ep-falling-cake-416088.eu-central-1.aws.neon.tech;Username=DAROMON;Database=neondb;Port=5432;Password=NVgYsqK8hyP6;SSLMode=Prefer;Timeout=10";
                var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();
                using (var command = new NpgsqlCommand($"SELECT COUNT(*) FROM acccounts WHERE login = @login AND password = @password;", conn))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", Md5(password));
                    result = (long)command.ExecuteScalar();
                }
                await conn.CloseAsync();
                if (result > 0) {
                    SaveAccount(login);
                    CheckLogin();
                }
                else
                {
                    Errors.Text += "Неверный логин или пароль\n";
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
                string connString = "Server=ep-falling-cake-416088.eu-central-1.aws.neon.tech;Username=DAROMON;Database=neondb;Port=5432;Password=NVgYsqK8hyP6;SSLMode=Prefer;Timeout=10";
                var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();
                using (var command = new NpgsqlCommand($"INSERT INTO acccounts (login, password) VALUES (@login, @password);", conn))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", Md5(password));
                    if(command.ExecuteNonQuery() == 1)
                    {
                        SaveAccount(login);
                        CheckLogin();
                        using (var command2 = new NpgsqlCommand($"CREATE TABLE IF NOT EXISTS {login} ( folder_name TEXT PRIMARY KEY, json_data TEXT);", conn))
                        {
                            command2.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        Errors.Text += "Ошибка регистрации\n";
                    }
                }
                await conn.CloseAsync();
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
    async void CheckLogin()
    {

        string login = await SecureStorage.Default.GetAsync("User");

        if (login == null)
        {
            notlogined.IsVisible = true;
            logined.IsVisible = false;
        }
        else
        {
            Hello.Text = $"Здравствуйте, {login}!";
            notlogined.IsVisible = false;
            logined.IsVisible = true;
        }
    }
}