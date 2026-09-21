using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;

/// <summary>
/// Agente 2 (cliente) SIN MonoBehaviour (clase pura).
/// Uso: await UGSCloudCodeManager.Instance.SubmitMatchResultAsync(result);
/// No requiere GameObject en escena. Configura endpoint/leaderboard por codigo o Remote Config.
/// </summary>
public class UGSCloudCodeManager
{
    public static readonly UGSCloudCodeManager Instance = new UGSCloudCodeManager();

    public string endpoint = "ValidateMatchResult";
    public string leaderboardId = "ranked_mmr";

    private UGSCloudCodeManager() { }

    [System.Serializable]
    public class MatchResult
    {
        public string winnerId;
        public string loserId;
        public int gridSize;
        public int totalMoves;
        public int winnerCells;
        public int loserCells;
    }

    [System.Serializable]
    public class RewardResponse
    {
        public bool valid;
        public int gemsAwarded;
        public int newMmr;
        public string message;
    }

    public async Task<RewardResponse> SubmitMatchResultAsync(MatchResult result)
    {
        if (AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("[CloudCode] Se requiere sesion.");
            return new RewardResponse { valid = false, message = "Sin sesion" };
        }
        try
        {
            var args = new Dictionary<string, object>
            {
                { "winnerId", result.winnerId },
                { "loserId", result.loserId },
                { "gridSize", result.gridSize },
                { "totalMoves", result.totalMoves },
                { "winnerCells", result.winnerCells },
                { "loserCells", result.loserCells }
            };
            var response = await CloudCodeService.Instance.CallEndpointAsync<RewardResponse>(endpoint, args);
            Debug.Log($"[CloudCode] valid={response.valid} gems={response.gemsAwarded} mmr={response.newMmr} msg={response.message}");

            if (response.valid)
            {
                bool iWon = result.winnerId == AuthenticationService.Instance.PlayerId;
                await UGSProfileManager.Instance.RecordMatchResultAsync(iWon);
            }
            return response;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CloudCode] Error endpoint {endpoint}: {e.Message}");
            return new RewardResponse { valid = false, message = e.Message };
        }
    }
}
