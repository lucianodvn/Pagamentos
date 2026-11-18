using Pagamentos.Domain.Enumerator;
using Pagamentos.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Domain.Entity
{
    public sealed class Pagamento
    {
        public const int ParcelasMaximasPadrao = 6;
        public const int ParcelasMaximasPremium = 12;

        public Guid Id { get; }
        public string NomeCliente { get; }
        public string EmailCliente { get; }
        public decimal Valor { get; }
        public MeioPagamento Meio { get; }
        public TipoCartaoCredito? TipoCartao { get; }
        public int Parcelas { get; }
        public string? CartaoUltimos4 { get; }
        public StatusPagamento Status { get; private set; }
        public string? ReferenciaExterna { get; private set; }
        public string? MotivoFalha { get; private set; }
        public DateTime CriadoEm { get; }
        public DateTime? ProcessadaEm { get; private set; }

        private Pagamento(Guid id, string nomeCliente, string emailCliente, decimal valor, MeioPagamento meio, TipoCartaoCredito? tipoCartao, int parcelas, string? cartaoUltimos4)
        {
            Id = id;
            NomeCliente = nomeCliente;
            EmailCliente = emailCliente;
            Valor = valor;
            Meio = meio;
            TipoCartao = tipoCartao;
            Parcelas = parcelas;
            CartaoUltimos4 = cartaoUltimos4;
            Status = StatusPagamento.Pendente;
            CriadoEm = DateTime.UtcNow;
        }

        public static Pagamento Criar(string nomeCliente, string emailCliente, decimal valor, MeioPagamento meio, TipoCartaoCredito? tipoCartao = null, int parcelas = 1, 
            string? cartaoUltimos4 = null)
        {
            if (string.IsNullOrWhiteSpace(nomeCliente))
                throw new ExcecaoDeDominio("O nome do cliente é obrigatório.");

            if (string.IsNullOrWhiteSpace(emailCliente) || !emailCliente.Contains("@"))
                throw new ExcecaoDeDominio("O e-mail do cliente é inválido.");

            if (valor <= 0)
                throw new ExcecaoDeDominio("O valor do pagamento deve ser maior que zero.");

            if (!Enum.IsDefined(meio))
                throw new ExcecaoDeDominio("Meio de pagamento inválido");

            switch(meio)
            {
                case MeioPagamento.Pix:
                    if (tipoCartao is not null || cartaoUltimos4 is not null)
                        throw new ExcecaoDeDominio("PIX não utiliza cartão.");
                    if (parcelas != 1)
                        throw new ExcecaoDeDominio("PIX não permite parcelamento.");
                    break;

                case MeioPagamento.CartaoDebito:
                    GarantirDigitosCartao(cartaoUltimos4);
                    if (tipoCartao is not null)
                        throw new ExcecaoDeDominio("Cartão de débito não possui tipo de crédito.");
                    if (parcelas != 1)
                        throw new ExcecaoDeDominio("Cartão de débito não permite parcelamento.");
                    break;

                case MeioPagamento.CartaoCredito:
                    GarantirDigitosCartao(cartaoUltimos4);
                    if (tipoCartao is null || !Enum.IsDefined(tipoCartao.Value))
                        throw new ExcecaoDeDominio("Informe o tipo do cartão de crédito (Padrão ou Premium).");
                    var maximo = tipoCartao == TipoCartaoCredito.Premium
                        ? ParcelasMaximasPremium
                        : ParcelasMaximasPadrao;
                    if (parcelas < 1 || parcelas > maximo)
                        throw new ExcecaoDeDominio($"Cartão {tipoCartao} permite de 1 a {maximo} parcelas.");
                    break;
            }

            return new Pagamento(Guid.NewGuid(), nomeCliente.Trim(), emailCliente.Trim(), decimal.Round(valor, 2), meio, tipoCartao, parcelas, cartaoUltimos4);
        }

        public void Aprovar(string referenciaExterna) 
        {
            GarantirPendente();
            Status = StatusPagamento.Aprovado;
            ReferenciaExterna = referenciaExterna;
            ProcessadaEm = DateTime.UtcNow;
        }

        public void Recusar(string motivo)
        {
            GarantirPendente();
            Status = StatusPagamento.Recusado;
            MotivoFalha = motivo;
            ProcessadaEm = DateTime.UtcNow;
        }

        private void GarantirPendente()
        {
            if (Status != StatusPagamento.Pendente)
                throw new ExcecaoDeDominio($"O pagamento já foi processado (status: {Status}).");
        }

        private static void GarantirDigitosCartao(string? ultimos4)
        {
            if (ultimos4 is null || ultimos4.Length != 4 || !ultimos4.All(char.IsDigit))
                throw new ExcecaoDeDominio("Informe os 4 últimos dígitos do cartão.");
        }
    }
}
