using Microsoft.Extensions.Http.Resilience;
using Yarp.ReverseProxy.Forwarder;

namespace Logistics.Gateway;

/// <summary>
/// YARP'ın varsayılan IForwarderHttpClientFactory implementasyonunu Polly dayanıklılık
/// pipeline'ı ile genişletir. Bu sayede Gateway'den backend servislere yapılan tüm
/// HTTP çağrıları otomatik Retry ve Circuit Breaker politikalarından geçer.
/// </summary>
public sealed class ResilientHttpClientFactory : IForwarderHttpClientFactory
{
    private readonly IHttpMessageHandlerFactory _handlerFactory;

    // YARP'ın backend çağrılarında kullanacağı named client adı
    private const string ClientName = "ResilientForwarder";

    public ResilientHttpClientFactory(IHttpMessageHandlerFactory handlerFactory)
    {
        _handlerFactory = handlerFactory;
    }

    /// <summary>
    /// YARP bu metodu her yeni proxy isteği için çağırır.
    /// Polly pipeline'ı üzerinden geçen HttpMessageInvoker döndürülür.
    /// </summary>
    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
    {
        // YARP, HttpClient nesnesini doğrudan KABUL ETMEZ. (ArgumentException fırlatır).
        // Bu yüzden IHttpMessageHandlerFactory kullanarak handler'ı alıp,
        // onu saf bir HttpMessageInvoker ile sarmalıyoruz.
        var handler = _handlerFactory.CreateHandler(ClientName);

        // disposeHandler: false (çünkü factory bu handler'ların yaşam döngüsünü yönetiyor)
        return new HttpMessageInvoker(handler, disposeHandler: false);
    }
}
