using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Domain.Exceptions
{
    public sealed class ExcecaoDeDominio(string mensagem) : Exception(mensagem);
}
