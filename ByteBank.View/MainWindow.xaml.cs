using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using ByteBank.View.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;
        private CancellationTokenSource _cts;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        private void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            //retornar TaskScheduler que está atuando no momento, nesse caso o da thread principal
            var taskSchedularUI = TaskScheduler.FromCurrentSynchronizationContext();

            BtnProcessar.IsEnabled = false;

            var contas = r_Repositorio.GetContaClientes();

            AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;

            ConsolidarContas(contas)
                .ContinueWith(task => { 
                    var fim = DateTime.Now;
                    var resultado = task.Result;
                    AtualizarView(resultado, fim - inicio);
                }, taskSchedularUI)
                .ContinueWith(task => {
                    BtnProcessar.IsEnabled = true;    
                }, taskSchedularUI);
        }


        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelar.IsEnabled = false;
            _cts.Cancel();
        }

        private Task<List<string>> ConsolidarContas(IEnumerable<ContaCliente> contas)
        {
            var resultado = new List<string>();

            var tasks = contas.Select(conta =>
                Task.Factory.StartNew(() =>
                {
                    var contaResultado = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(contaResultado);
                })
            );

            return Task.WhenAll(tasks).ContinueWith(t =>
            {
                return resultado;
            });
        }

        private void LimparView()
        {
            LstResultados.ItemsSource = null;
            TxtTempo.Text = null;
            PgsProgresso.Value = 0;
        }

        private void AtualizarView(IEnumerable<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count()} clientes em {tempoDecorrido}";

             LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }
    }
}
