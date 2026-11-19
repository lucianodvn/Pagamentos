using Pagamentos.Application.Abstraction;
using Pagamentos.Domain.Entity;
using Pagamentos.Domain.Enumerator;

namespace Pagamentos.Infrastructure.Gateways;

public sealed class GatewayPixSimulado : IGatewayPagamento
{
    private const decimal Limite = 10_000m;

    public bool Suporta(Pagamento pagamento) => pagamento.Meio == MeioPagamento.Pix;

    public async Task<ResultadoGateway> AutorizarAsync(Pagamento pagamento, CancellationToken ct = default)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500), ct); // simula a confirmação do PIX

        return pagamento.Valor > Limite
            ? new ResultadoGateway(false, MotivoFalha: $"Valor acima do limite PIX simulado (R$ {Limite:N2}).")
            : new ResultadoGateway(true, Referencia: $"PIX-SIMULADO-{Guid.NewGuid():N}");
    }
}

public sealed class GatewayDebitoSimulado : IGatewayPagamento
{
    private const decimal Limite = 3_000m;

    public bool Suporta(Pagamento pagamento) => pagamento.Meio == MeioPagamento.CartaoDebito;

    public async Task<ResultadoGateway> AutorizarAsync(Pagamento pagamento, CancellationToken ct = default)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(700), ct);

        if (pagamento.Valor > Limite)
            return new ResultadoGateway(false, MotivoFalha: $"Valor acima do limite de débito simulado (R$ {Limite:N2}).");

        return RegrasCartao.Avaliar(pagamento, "DEBITO");
    }
}

public sealed class GatewayCreditoSimulado : IGatewayPagamento
{
    private const decimal LimitePadrao = 5_000m;
    private const decimal LimitePremium = 20_000m;

    public bool Suporta(Pagamento pagamento) => pagamento.Meio == MeioPagamento.CartaoCredito;

    public async Task<ResultadoGateway> AutorizarAsync(Pagamento pagamento, CancellationToken ct = default)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(900), ct);

        var limite = pagamento.TipoCartao == TipoCartaoCredito.Premium ? LimitePremium : LimitePadrao;
        if (pagamento.Valor > limite)
            return new ResultadoGateway(false,
                MotivoFalha: $"Valor acima do limite do cartão {pagamento.TipoCartao} simulado (R$ {limite:N2}).");

        return RegrasCartao.Avaliar(pagamento, $"CREDITO-{pagamento.TipoCartao}".ToUpperInvariant());
    }
}

internal static class RegrasCartao
{
    public static ResultadoGateway Avaliar(Pagamento pagamento, string prefixo) => pagamento.CartaoUltimos4 switch
    {
        "0002" => new ResultadoGateway(false, MotivoFalha: "Cartão recusado (simulado)."),
        "0051" => new ResultadoGateway(false, MotivoFalha: "Saldo/limite insuficiente (simulado)."),
        _ => new ResultadoGateway(true, Referencia: $"{prefixo}-SIMULADO-{Guid.NewGuid():N}")
    };
}