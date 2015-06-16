﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AEGF.Dominio;
using AEGF.Dominio.Servicos;
using AEGF.Infra;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace AEGF.BancosViaSite
{
    public class CEFSite: AcessoSelenium, IBancoAcesso
    {
        private Banco _banco;
        private ICollection<Extrato> _extratos;

        protected override IWebDriver CriarBrowser()
        {
            var f = new FirefoxProfileManager();
            var p = f.GetProfile("default");
            
            return new FirefoxDriver(p);            
        }

        public IEnumerable<Extrato> LerExtratos()
        {
            IniciarBrowser();
            Inicio();
            FazerLogin();
            LerExtrato();
            FecharBrowser();
            return _extratos;
        }


        private void LerExtrato()
        {
            Tempo();
            ClicaXPath("//nav[@id='carousel-home']/div/ul/li[1]/a/img");
            ClicaXPath("(//div[contains(@class, 'floatdiv')])[1]/div[3]/a");
            Tempo();
            SelecionaMesAtual();
            var numConta = LerNumeroConta();
            LerTabelaExtrato(numConta, DateTime.Today.PrimeiroDia());
            Tempo();
            ClicaXPath("//nav[@id='carousel-internas']/div/ul/li[1]/a/img");
            ClicaXPath("(//div[contains(@class, 'floatdiv')])[1]/div[3]/a");
            Tempo();
            SelecionaMesAnterior();
            LerTabelaExtrato(numConta, DateTime.Today.AddMonths(-1).PrimeiroDia());
        }

        private string LerNumeroConta()
        {
            return LerTextoCSS("#contaSelecionada li.conta strong");
        }

        private void Tempo()
        {
            // Site da CEF é temperamental, se for muito rápido ele dá erro
            Thread.Sleep(new TimeSpan(0, 0, 5));
        }

        private void LerTabelaExtrato(string numConta, DateTime referencia)
        {
            AguardarCSS("table.movimentacao");
            var extrato = CriaRetorno("table.movimentacao tr", false, 0, 2, 3);
            extrato.Referencia = referencia;
            extrato.Descricao = String.Format("CEF Conta Corrente - {0}", numConta);
            _extratos.Add(extrato);

        }

        private Extrato CriaRetorno(string cssPath, bool tipoCartao, int colData, int colDescricao, int colValor)
        {
            var trs = driver.FindElements(By.CssSelector(cssPath));
            var retorno = new Extrato { CartaoCredito = tipoCartao };

            AdicionaItens(retorno, trs, colData, colDescricao, colValor);

            return retorno;
        }

        private static void AdicionaItens(Extrato extrato, ReadOnlyCollection<IWebElement> linhas, int colData, int colDescricao, int colValor)
        {
            var linhaAtual = 0;

            foreach (var linha in linhas)
            {
                linhaAtual++;

                var colunas = linha.FindElements(By.TagName("td"));

                if (linhaAtual == 3)
                    extrato.SaldoAnterior = BuscaValor(colunas, colValor + 1);

                if (linhaAtual <= 3)
                    continue;                



                var valor = BuscaValor(colunas, colValor);

                if (valor != 0)
                {
                    var item = new Transacao()
                    {
                        Valor = valor,
                        Descricao = colunas[colDescricao].Text,
                        Data = DateTime.Parse(colunas[colData].Text)
                    };
                    extrato.AdicionaTransacao(item);

                }
            }
        }

        private static double BuscaValor(ReadOnlyCollection<IWebElement> colunas, int colValor)
        {
            var colunaValor = colunas[colValor].Text.Split(' ');
            var valorStr = colunaValor[0];
            double valor;

            if (Double.TryParse(valorStr, out valor))
            {                
                if ((valor != 0) && (colunaValor[1] == "D"))
                    valor *= -1;
            }
            return valor;
        }


        private void SelecionaMesAtual()
        {
            ConfirmaMesExtrato();
        }

        private void ConfirmaMesExtrato()
        {
            const string id = "confirma";
            AguardarId(id);
            ClicaId(id);
        }

        private void SelecionaMesAnterior()
        {
            const string id = "rdoTipoExtratoOutro";
            AguardarId(id);
            ClicaId(id);
            Tempo();
            ClicaXPath("//*[@id=\"dk_container_sltOutroMes\"]/a");
            Tempo();
            ClicaXPath("//*[@id=\"dk_container_sltOutroMes\"]/div/ul/li[2]/a");
            ConfirmaMesExtrato();
        }

        private void FazerLogin()
        {
            
            DigitaTextoId("usuario", _banco.LerConfiguracao("usuario"));
            ClicaXPath("//*[@id=\"divPF\"]/input");
            ClicaXPath("//*[@id=\"botaoAcessar\"]/button/span");
            ClicaId("iniciais");
            // Usuario tem que digitar a senha
            AguardarXPath("//div[contains(@class, 'keyboard-button') and text()='a']");

            var senha = _banco.LerConfiguracao("senha");

            foreach (var letra in senha)
            {
                var elemento =
                    driver.FindElement(
                        By.XPath(String.Format("//div[contains(@class, 'keyboard-button') and text()='{0}']", letra)));
                Actions builder = new Actions(driver);
                builder.MoveToElement(elemento).Click().Perform();
            }
            var botao =
                driver.FindElement(
                    By.Id("85Confirm"));
            Actions acaoBotao = new Actions(driver);
            acaoBotao.MoveToElement(botao).Click().Perform();
            //ClicaCSS("button.button-blue");
            AguardarXPath("//nav[@id='carousel-home']/div/ul/li[1]/a/img");
        }


        private void Inicio()
        {
            _extratos = new List<Extrato>();
        }

        public string NomeUnico()
        {
            return "CEFSite";
        }

        public void Iniciar(Banco banco)
        {
            _banco = banco;
        }

        protected override string URLSite()
        {
            return "https://internetbanking.caixa.gov.br/SIIBC/index.processa";
        }
    }
}