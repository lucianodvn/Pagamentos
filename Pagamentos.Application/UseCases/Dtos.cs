using Pagamentos.Domain.Entity;
using Pagamentos.Domain.Enumerator;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Application.UseCases
{
    public sealed record CriarPagamentoRequisicao(
            string NomeCliente,
            string EmailCliente,
            decimal Valor,
            MeioPagamento Meio,
            TipoCartaoCredito? TipoCartao = null,
            int Parcelas = 1,
            string? NumeroCartao = null
    );

    public sealed record CriarPagamentoResposta(Guid Id, StatusPagamento Status);

    public sealed record PagamentoDto(
        Guid Id,
        string NomeCliente,
        decimal Valor,
        MeioPagamento Meio,
        TipoCartaoCredito? TipoCartao,
        int Parcelas,
        string? CartaoUltimos4,
        StatusPagamento Status,
        string? ReferenciaExterna,
        string? MotivoFalha,
        DateTime CriadoEm,
        DateTime? ProcessadoEm
    )
    {
        public static PagamentoDto De(Pagamento pagamento) => new(
                pagamento.Id,
                pagamento.NomeCliente,
                pagamento.Valor,
                pagamento.Meio,
                pagamento.TipoCartao,
                pagamento.Parcelas,
                pagamento.CartaoUltimos4,
                pagamento.Status,
                pagamento.ReferenciaExterna,
                pagamento.MotivoFalha,
                pagamento.CriadoEm,
                pagamento.ProcessadaEm
        );
    }
}

