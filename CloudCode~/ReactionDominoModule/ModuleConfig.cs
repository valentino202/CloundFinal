using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace ReactionDomino.CloudCode
{
    /// <summary>
    /// Punto de entrada de la inyeccion de dependencias del modulo: registra <see cref="IGameApiClient"/>,
    /// que es lo que usan las funciones para hablar con Cloud Save. <see cref="IPushClient"/> no hace
    /// falta registrarlo: Cloud Code lo inyecta solo en cualquier funcion que lo pida como parametro.
    /// </summary>
    public class ModuleConfig : ICloudCodeSetup
    {
        public void Setup(ICloudCodeConfig config)
        {
            config.AddGameApiClient();
        }
    }
}
