using Pagamentos.Domain.Entity;
using Pagamentos.Domain.Enumerator;
using Pagamentos.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Tests
{
    public class PagamentoTests
    {
        [Fact]
        public void Pix_valido_nasce_pendente()
        {
            var pagamento = Pagamento.Criar("Maria", "maria@x.com", 100m, MeioPagamento.Pix);

            Assert.Equal(StatusPagamento.Pendente, pagamento.Status);
        }

        [Theory]
        [InlineData(TipoCartaoCredito.Padrao, 6, true)]
        [InlineData(TipoCartaoCredito.Padrao, 7, false)]
        [InlineData(TipoCartaoCredito.Premium, 12, true)]
        [InlineData(TipoCartaoCredito.Premium, 13, false)]
        public void Parcelas_respeitam_o_limite_do_tipo_de_cartao(TipoCartaoCredito tipo, int parcelas, bool valido)
        {
            void Acao() => Pagamento.Criar("Maria", "maria@x.com", 1000m, MeioPagamento.CartaoCredito,
                tipo, parcelas, "4242");

            if (valido) Acao();
            else Assert.Throws<ExcecaoDeDominio>(Acao);
        }

        [Fact]
        public void Pagamento_processado_nao_pode_ser_processado_de_novo()
        {
            var pagamento = Pagamento.Criar("Maria", "maria@x.com", 100m, MeioPagamento.Pix);
            pagamento.Aprovar("REF-1");

            Assert.Equal(StatusPagamento.Aprovado, pagamento.Status);
            Assert.Throws<ExcecaoDeDominio>(() => pagamento.Recusar("tarde demais"));
        }
    }
}
