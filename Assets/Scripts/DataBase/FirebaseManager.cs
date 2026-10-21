using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Messaging;

public class FirebaseManager : MonoBehaviour
{
    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Firebase 초기화 작업 실패");
                    return;
                }

                if (task.Result == DependencyStatus.Available)
                {
                    Debug.Log("Firebase 초기화 성공!");

                    FirebaseMessaging.RegistrationReceived
                        += OnRegistrationReceived;

                    FirebaseMessaging.MessageReceived
                        += OnMessageReceived;

                    GetFCMToken();
                }
                else
                {
                    Debug.LogError(
                        "Firebase 의존성 오류: " + task.Result);
                }
            });
    }

    private async void OnRegistrationReceived(
        object sender, RegistrationReceivedEventArgs e)
    {
        try
        {
            string token = await FirebaseMessaging.GetTokenAsync();
            Debug.Log("FCM Token: " + token);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("FCM 토큰 발급 실패: " + ex.Message);
        }
    }

    private void OnMessageReceived(
        object sender, MessageReceivedEventArgs e)
    {
        Debug.Log("메시지 수신: " +
            e.Message.Notification?.Title);
    }

    private void OnDestroy()
    {
        FirebaseMessaging.RegistrationReceived
            -= OnRegistrationReceived;

        FirebaseMessaging.MessageReceived
            -= OnMessageReceived;
    }

    private async void GetFCMToken()
    {
        try
        {
            string token = await FirebaseMessaging.GetTokenAsync();
            Debug.Log("FCM Token: " + token);
        }
        catch (System.Exception e)
        {
            Debug.LogError("FCM 토큰 발급 실패: " + e.Message);
        }
    }
}