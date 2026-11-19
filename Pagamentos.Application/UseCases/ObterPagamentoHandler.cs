using Pagamentos.Application.Abstraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Application.UseCases
{
    public sealed class ObterPagamentoHandler(IRepositorioPagamento repositorio)
    {
        public async Task<PagamentoDto?> ExecutarAsync(Guid id, CancellationToken ct = default)
        {
            var pagamento = await repositorio.ObterPorIdAsync(id, ct);
            return pagamento is null ? null : PagamentoDto.De(pagamento);
        }
    }
}
