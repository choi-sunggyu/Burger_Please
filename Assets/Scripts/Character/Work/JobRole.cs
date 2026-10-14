[System.Flags]
public enum JobRole
{
    None    = 0,
    Clean   = 1 << 0,   // 1  (0001)
    Deliver = 1 << 1,   // 2  (0010)
    Pack    = 1 << 2,   // 4  (0100)
    Sell    = 1 << 3,   // 8  (1000)
    All     = Clean | Deliver | Pack | Sell   // 15 (1111)
}