using Pagamentos.Application.Abstraction;
using Pagamentos.Application.Messages;
using Pagamentos.Domain.Enumerator;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Application.UseCases
{
    public class ProcessarPagamentoHandler(IRepositorioPagamento repositorio, IEnumerable<IGatewayPagamento> gateways, IPublicarMensagens publicar)
    {
        public async Task ExecutarAsync(PagamentoSolicitadoMensagem mensagem, CancellationToken ct = default)
        {
            var pagamento = await repositorio.ObterPorIdAsync(mensagem.PagamentoId, ct);
            if (pagamento is null || pagamento.Status != StatusPagamento.Pendente)
                return;

            var gateway = gateways.FirstOrDefault(g => g.Suporta(pagamento));

            var resultado = gateway is null
                ? new ResultadoGateway(false, MotivoFalha: "Meio de pagamento não suportado.")
                : await gateway.AutorizarAsync(pagamento, ct);

            if(resultado.Aprovado)
                pagamento.Aprovar(resultado.Referencia ?? string.Empty);
            else
                pagamento.Recusar(resultado.MotivoFalha ?? "Pagamento recusado.");

            await repositorio.AtualizarAsync(pagamento, ct);

            await publicar.PublicarAsync(Filas.PagamentoProcessado, new PagamentoProcessadoMensagem(pagamento.Id, resultado.Aprovado, resultado.MotivoFalha), ct);
        }
    }
}
