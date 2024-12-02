using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace HospitalDatabaseInterface
{
    public partial class MainForm : Form
    {
        private MySqlConnection connection;
        private DataTable dataTable;
        private MySqlDataAdapter adapter;
        private string selectedTable;

        public MainForm()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
        }

        private void InitializeDatabaseConnection()
        {
            string server = "127.0.0.1"; // SSH тунель
            string database = "лікарня";
            string username = "root";
            string password = "1qaz@WSX";

            string connectionString = $"Server={server};Database={database};Uid={username};Pwd={password};Charset=utf8mb4;";
            connection = new MySqlConnection(connectionString);
        }

        private void InitializeCommands()
        {
            string selectQuery = $"SELECT * FROM {selectedTable};";
            adapter = new MySqlDataAdapter(selectQuery, connection);

            // Створюємо команду для оновлення
            MySqlCommandBuilder commandBuilder = new MySqlCommandBuilder(adapter);

            // Встановлюємо адаптер, щоб він використовував ці команди
            adapter.UpdateCommand = commandBuilder.GetUpdateCommand();
            adapter.InsertCommand = commandBuilder.GetInsertCommand();
            adapter.DeleteCommand = commandBuilder.GetDeleteCommand();
        }

        private void LoadTable(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                MessageBox.Show("Виберіть таблицю для завантаження.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                selectedTable = tableName;
                connection.Open();
                string query = $"SELECT * FROM {tableName};";

                adapter = new MySqlDataAdapter(query, connection);
                InitializeCommands(); // Ініціалізація команд адаптера

                dataTable = new DataTable();
                adapter.Fill(dataTable);
                dataGridView.DataSource = dataTable;

                connection.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження таблиці: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                connection.Close();
            }
        }

        private void SaveChanges()
        {
            try
            {
                // Оновлюємо зміни в базі даних
                adapter.Update(dataTable);
                MessageBox.Show("Зміни успішно збережено.", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження змін: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddRow()
        {
            if (dataTable == null)
            {
                MessageBox.Show("Спочатку завантажте таблицю.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Створюємо новий рядок
                DataRow newRow = dataTable.NewRow();

                // Перебираємо всі стовпці в таблиці
                foreach (DataColumn column in dataTable.Columns)
                {
                    // Встановлюємо значення для стовпців
                    if (column.ColumnName.Equals("ID", StringComparison.OrdinalIgnoreCase))
                    {
                        newRow[column.ColumnName] = DBNull.Value; // Автоматична генерація ID
                    }
                    else if (column.DataType == typeof(string))
                    {
                        newRow[column.ColumnName] = "Текстове значення"; // Приклад текстового значення
                    }
                    else if (column.DataType == typeof(DateTime))
                    {
                        newRow[column.ColumnName] = DateTime.Now; // Поточна дата
                    }
                    else if (column.DataType == typeof(int) || column.DataType == typeof(long))
                    {
                        newRow[column.ColumnName] = 0; // Числове значення за замовчуванням
                    }
                    else
                    {
                        newRow[column.ColumnName] = DBNull.Value; // Значення за замовчуванням
                    }
                }

                // Додаємо новий рядок до таблиці
                dataTable.Rows.Add(newRow);

                // Оновлюємо таблицю в базі даних
                adapter.Update(dataTable);
                MessageBox.Show("Рядок успішно додано.", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка додавання рядка: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void DeleteRow()
        {
            if (dataTable == null || dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Немає рядків для видалення або таблиця не завантажена.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                foreach (DataGridViewRow row in dataGridView.SelectedRows)
                {
                    if (!row.IsNewRow)
                    {
                        DataRow dataRow = ((DataRowView)row.DataBoundItem).Row;
                        dataRow.Delete(); // Видаляємо рядок з DataTable
                    }
                }

                // Оновлюємо таблицю в базі даних після видалення
                adapter.Update(dataTable);
                MessageBox.Show("Рядок(и) успішно видалено.", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка видалення рядка: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateID(int oldID, int newID)
        {
            try
            {
                // Оновлюємо ID вручну за допомогою SQL запиту
                string updateQuery = $"UPDATE {selectedTable} SET ID = @NewID WHERE ID = @OldID;";
                MySqlCommand updateCommand = new MySqlCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@NewID", newID);
                updateCommand.Parameters.AddWithValue("@OldID", oldID);

                connection.Open();
                updateCommand.ExecuteNonQuery();
                connection.Close();

                // Оновлюємо таблицю після зміни ID
                LoadTable(selectedTable);
                MessageBox.Show("ID успішно оновлено.", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка оновлення ID: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoadTable_Click(object sender, EventArgs e)
        {
            string tableName = cmbTables.SelectedItem?.ToString();
            LoadTable(tableName);
        }

        private void btnAddRow_Click(object sender, EventArgs e)
        {
            AddRow();
        }

        private void btnDeleteRow_Click(object sender, EventArgs e)
        {
            DeleteRow();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            cmbTables.Items.AddRange(new string[] { "Пацієнти", "Лікарі", "Прийоми", "Медичні_записи" });
        }

        private void dataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Обробник кліку по клітинках DataGridView (за потреби)
        }

        private void btnSaveChanges_Click(object sender, EventArgs e)
        {
            SaveChanges();
        }

        private void dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (dataTable != null)
            {
                try
                {
                    // Збереження змін в базі даних
                    adapter.Update(dataTable);
                    MessageBox.Show("Зміни успішно збережено.", "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка збереження змін: {ex.Message}", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
