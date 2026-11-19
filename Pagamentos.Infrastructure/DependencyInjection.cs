using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pagamentos.Application.Abstraction;
using Pagamentos.Infrastructure.Gateways;
using Pagamentos.Infrastructure.Messaging;
using Pagamentos.Infrastructure.Notifications;
using Pagamentos.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

public static class DependencyInjection
{
    public static IServiceCollection AdicionarInfraestrutura(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.Configure<OpcoesRabbitMq>(configuracao.GetSection(OpcoesRabbitMq.NomeSecao));

        // Persistência
        servicos.AddSingleton<IRepositorioPagamento, RepositorioPagamentoEmMemoria>();

        // Gateways simulados (um por meio de pagamento)
        servicos.AddSingleton<IGatewayPagamento, GatewayPixSimulado>();
        servicos.AddSingleton<IGatewayPagamento, GatewayDebitoSimulado>();
        servicos.AddSingleton<IGatewayPagamento, GatewayCreditoSimulado>();

        // Notificação ao cliente
        servicos.AddSingleton<INotificadorCliente, NotificadorClienteConsole>();

        // Mensageria
        servicos.AddSingleton<ConexaoRabbitMq>();
        servicos.AddSingleton<IPublicarMensagens, PublicadorRabbitMq>();
        servicos.AddHostedService<ConsumidorPagamentoSolicitado>();
        servicos.AddHostedService<ConsumidorPagamentoProcessado>();

        return servicos;
    }
}
