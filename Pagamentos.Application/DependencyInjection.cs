using Microsoft.Extensions.DependencyInjection;
using Pagamentos.Application.UseCases;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AdicionarAplicacao(this IServiceCollection services)
        {
            services.AddScoped<CriarPagamentoHandler>();
            services.AddScoped<ObterPagamentoHandler>();
            services.AddScoped<NotificarResultadoHandler>();
            return services;
        }
    }
}
