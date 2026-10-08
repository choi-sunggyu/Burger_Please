// 플레이어가 손에 든 Stuff를 내려놓을 수 있는 곳(CubeSpot, TrashBox 등 나중에는 커피도 하더만)이 구현하는 인터페이스 (TrashBox 는 ThrowTrash 가 따로 처리)
public interface IStuffReceiver
{
    bool CanReceive(StuffType type);   // 이 종류를 지금 받을 수 있는가?
    void Receive(Stuff stuff);         // 실제로 받아서 처리
}
