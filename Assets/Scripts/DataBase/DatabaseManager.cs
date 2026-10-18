
using System;
using UnityEngine;
using MySqlConnector;

public class DatabaseManager : MonoBehaviour
{
    private string connectionString =
        "Server=127.0.0.1;Port=3306;" +
        "Database=unity_test;" +
        "User ID=unity_app;" +
        "Password=choi;";

    private void Start()
    {
        try
        {
            using var connection =
                new MySqlConnection(connectionString);

            connection.Open();
            Debug.Log("MySQL 연결 성공!");

            // 1. 기존 데이터 조회
            LoadMoney(connection, 1);

            // 2. 데이터 수정
            SaveMoney(connection, 1, 2000);

            // 3. 수정된 데이터 확인
            LoadMoney(connection, 1);
        }
        catch (MySqlException ex)
        {
            Debug.LogError($"MySQL 오류: {ex.Message}");
        }
    }

    private void LoadMoney(MySqlConnection connection, int playerId)
    {
        string sql =
            "SELECT money FROM player WHERE id = @id";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", playerId);

        object result = command.ExecuteScalar();

        if (result != null && result != DBNull.Value)
        {
            int money = Convert.ToInt32(result);
            Debug.Log($"플레이어 {playerId} 보유 금액: {money}");
        }
    }

    private void SaveMoney(MySqlConnection connection, int playerId, int money)
    {
        string sql =
            "UPDATE player SET money = @money WHERE id = @id";

        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@money", money);
        command.Parameters.AddWithValue("@id", playerId);

        int rows = command.ExecuteNonQuery();

        Debug.Log($"데이터 저장 완료: 영향받은 행 {rows}개");
    }
}
