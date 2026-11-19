using Pagamentos.Application.Abstraction;
using Pagamentos.Application.Messages;
using Pagamentos.Domain.Entity;
using Pagamentos.Domain.Enumerator;
using Pagamentos.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Application.UseCases
{
    public sealed class CriarPagamentoHandler(IRepositorioPagamento repositorio, IPublicarMensagens publicar)
    {
        public async Task<CriarPagamentoResposta> ExecutarAsync(CriarPagamentoRequisicao requisicao, CancellationToken ct = default)
        {
            string? ultimos4 = null;

            if (requisicao.Meio is MeioPagamento.CartaoDebito or MeioPagamento.CartaoCredito)
            {
                var digitos = new string((requisicao.NumeroCartao ?? string.Empty).Where(char.IsDigit).ToArray());
                if(digitos.Length is < 13 or > 19)
                {
                    throw new ExcecaoDeDominio("Número do cartão inválido.");
                }

                ultimos4 = digitos[^4..];
            }

            var pagamento = Pagamento.Criar(
                requisicao.NomeCliente,
                requisicao.EmailCliente,
                requisicao.Valor,
                requisicao.Meio,
                requisicao.TipoCartao,
                requisicao.Parcelas,
                ultimos4
            );

            await repositorio.AdicionarAsync(pagamento, ct);
            await publicar.PublicarAsync(Filas.PagamentoSolicitado, new PagamentoSolicitadoMensagem(pagamento.Id), ct);

            return new CriarPagamentoResposta(pagamento.Id, pagamento.Status);
        }
     }
}
