using Pagamentos.Application.Abstraction;
using Pagamentos.Application.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Application.UseCases
{
    public sealed class NotificarResultadoHandler(IRepositorioPagamento repositorio, INotificadorCliente notificador)
    {
        public async Task ExecutarAsync(PagamentoProcessadoMensagem mensagem, CancellationToken ct = default)
        {
            var pagamento = await repositorio.ObterPorIdAsync(mensagem.PagamentoId, ct);
            if(pagamento is null)
                return;

            if(mensagem.Aprovado)
                await notificador.NotificarPagamentoAprovadoAsync(pagamento, ct);
            else
                await notificador.NotificarPagamentoRecusadoAsync(pagamento, ct);
        }
    }
}
