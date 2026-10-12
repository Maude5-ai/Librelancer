using System.Collections.Generic;
using LibreLancer.Net;

namespace LLServer;

public class ServerConfig
{
    public string ServerName = "";
    public string ServerDescription = "";
    public string FreelancerPath = "";
    public string? LoginUrl;
    public string DatabasePath = "";
    public int Port = LNetConst.DefaultPort;
    public int ThreadCount = 0;
    public int MaxAccountCharacters = 10;
    public bool EnableRateLimiting = true;
    // Character name -> mission INI path relative to the game data folder
    // (e.g. "MISSIONS\\CUSTOM\\mission.ini"). The mission starts for that
    // character on login; runs entirely server-side over the existing protocol.
    public Dictionary<string, string> CharacterMissions = new();

    public void CopyFrom(ServerConfig other)
    {
        ServerName = other.ServerName;
        ServerDescription = other.ServerDescription;
        FreelancerPath= other.FreelancerPath;
        LoginUrl = other.LoginUrl;
        DatabasePath = other.DatabasePath;
        Port = other.Port;
        ThreadCount = other.ThreadCount;
        MaxAccountCharacters = other.MaxAccountCharacters;
        EnableRateLimiting = other.EnableRateLimiting;
        CharacterMissions = other.CharacterMissions;
    }
}
