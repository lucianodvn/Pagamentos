using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Application.Messages
{
    public static class Filas
    {
        public const string PagamentoSolicitado = "pagamentos.solicitado";
        public const string PagamentoProcessado = "pagamentos.processado";
    }

    public sealed record PagamentoSolicitadoMensagem(Guid PagamentoId);

    public sealed record PagamentoProcessadoMensagem(Guid PagamentoId, bool Aprovado, string? Motivo);
}
