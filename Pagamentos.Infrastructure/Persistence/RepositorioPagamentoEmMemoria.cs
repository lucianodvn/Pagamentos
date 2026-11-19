using System.Collections.Concurrent;
using Pagamentos.Application.Abstraction;
using Pagamentos.Domain.Entity;

namespace Pagamentos.Infrastructure.Persistence;

public sealed class RepositorioPagamentoEmMemoria : IRepositorioPagamento
{
    private readonly ConcurrentDictionary<Guid, Pagamento> _dados = new();

    public Task AdicionarAsync(Pagamento pagamento, CancellationToken ct = default)
    {
        _dados[pagamento.Id] = pagamento;
        return Task.CompletedTask;
    }

    public Task<Pagamento?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_dados.TryGetValue(id, out var pagamento) ? pagamento : null);

    public Task AtualizarAsync(Pagamento pagamento, CancellationToken ct = default)
    {
        _dados[pagamento.Id] = pagamento;
        return Task.CompletedTask;
    }
}