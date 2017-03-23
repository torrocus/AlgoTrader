﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Data.Sql;
using System.IO;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.Threading;

namespace LiveAlgo
{
    public partial class AlgoForm : Form
    {

        private SterlingLib.STIApp stiApp = new SterlingLib.STIApp();
        private SterlingLib.STIEvents stiEvents = new SterlingLib.STIEvents();
        private SterlingLib.STIOrder stiOrder = new SterlingLib.STIOrder();
        private SterlingLib.STIPosition stiPos = new SterlingLib.STIPosition();
        private SterlingLib.STIQuote stiQuote = new SterlingLib.STIQuote();
        private SterlingLib.ISTIOrderMaint orderMaint = new SterlingLib.STIOrderMaint();

        private SymbolAlgo testAlgo = new SymbolAlgo();

        public AlgoForm()
        {
            InitializeComponent();
            stiApp.SetModeXML(true);
            stiEvents.SetOrderEventsAsStructs(true);
            stiEvents.OnSTIOrderUpdateXML += new SterlingLib._ISTIEventsEvents_OnSTIOrderUpdateXMLEventHandler(OnSTIOrderUpdateXML);

        }

        private void OnSTIOrderUpdateXML(ref string strOrder)
        {
            if (testAlgo.status == "Stopped")
            {
                return;
            }

            XmlSerializer xs = new XmlSerializer(typeof(SterlingLib.structSTIOrderUpdate));
            SterlingLib.structSTIOrderUpdate structOrder = (SterlingLib.structSTIOrderUpdate)xs.Deserialize(new StringReader(strOrder));

            if (structOrder.bstrSymbol.ToUpper() == testAlgo.symbol.ToUpper() && structOrder.nOrderStatus == 5)
            {   //Limit Order has been filled 
                if (Convert.ToDecimal(structOrder.fLmtPrice) < testAlgo.midPrice)  //Buy order has been filled
                {
                    testAlgo.buyFills++;

                    //Shift orders
                    //Normal behaviour, if lists are full
                    if (testAlgo.ordersAbove.Count == 5 && testAlgo.ordersBelow.Count == 5)
                    {
                        testAlgo.midPrice = Convert.ToDecimal(structOrder.fLmtPrice);

                        //Move filled order to completed list, remove from active list
                        SterlingLib.ISTIOrder filledOrder = testAlgo.ordersAbove.Find(i => i.ClOrderID == structOrder.bstrClOrderId);
                        testAlgo.ordersBelow.Remove(filledOrder);
                        testAlgo.filledOrders.Add(filledOrder);

                        //Insert new order below 5 orders away

                        SterlingLib.STIOrder stiOrder = new SterlingLib.STIOrder();
                        stiOrder.Symbol = testAlgo.symbol;
                        stiOrder.Account = Globals.account;
                        stiOrder.Side = "B";
                        stiOrder.Quantity = testAlgo.incrementSize;
                        stiOrder.Tif = "D"; //day order
                        stiOrder.PriceType = SterlingLib.STIPriceTypes.ptSTILmt;
                        stiOrder.LmtPrice = Convert.ToDouble(testAlgo.midPrice - (testAlgo.incrementPrice * 5));
                        stiOrder.Destination = "BATS";
                        stiOrder.ClOrderID = Guid.NewGuid().ToString();

                        int orderStatus = stiOrder.SubmitOrder();
                        if (orderStatus != 0)
                        {
                            MessageBox.Show("Order Error: " + orderStatus.ToString());
                        }
                        else
                        {
                            //Add to appropriate list
                            testAlgo.ordersBelow.Insert(0, stiOrder);
                        }

                        //Cancel the "6th" order from opposite side order list

                        SterlingLib.ISTIOrder cancelOrder = testAlgo.ordersAbove[testAlgo.ordersAbove.Count - 1];
                        orderMaint.CancelOrder(Globals.account, 0, cancelOrder.ClOrderID, Guid.NewGuid().ToString());
                        testAlgo.ordersAbove.RemoveAt(testAlgo.ordersAbove.Count - 1);

                        //Place new order in opposite side order list

                        stiOrder = new SterlingLib.STIOrder();
                        stiOrder.Symbol = testAlgo.symbol;
                        stiOrder.Account = Globals.account;
                        stiOrder.Side = "S";
                        stiOrder.Quantity = testAlgo.incrementSize;
                        stiOrder.Tif = "D"; //day order
                        stiOrder.PriceType = SterlingLib.STIPriceTypes.ptSTILmt;
                        stiOrder.LmtPrice = Convert.ToDouble(testAlgo.midPrice + (testAlgo.incrementPrice));
                        stiOrder.Destination = "BATS";
                        stiOrder.ClOrderID = Guid.NewGuid().ToString();

                        orderStatus = stiOrder.SubmitOrder();
                        if (orderStatus != 0)
                        {
                            MessageBox.Show("Order Error: " + orderStatus.ToString());
                        }
                        else
                        {
                            //Add to appropriate list
                            testAlgo.ordersAbove.Insert(0, stiOrder);
                        }
                    }

                }
                else if (Convert.ToDecimal(structOrder.fLmtPrice) > testAlgo.midPrice)
                {
                    testAlgo.sellFills++;

                    //Shift orders
                    //Normal behaviour, if lists are full
                    if (testAlgo.ordersAbove.Count == 5 && testAlgo.ordersBelow.Count == 5)
                    {
                        testAlgo.midPrice = Convert.ToDecimal(structOrder.fLmtPrice);

                        //Move filled order to completed list, remove from active list
                        SterlingLib.ISTIOrder filledOrder = testAlgo.ordersAbove.Find(i => i.ClOrderID == structOrder.bstrClOrderId);
                        testAlgo.ordersAbove.Remove(filledOrder);
                        testAlgo.filledOrders.Add(filledOrder);

                        //Insert new order above 5 orders away

                        SterlingLib.STIOrder stiOrder = new SterlingLib.STIOrder();
                        stiOrder.Symbol = testAlgo.symbol;
                        stiOrder.Account = Globals.account;
                        stiOrder.Side = "S";
                        stiOrder.Quantity = testAlgo.incrementSize;
                        stiOrder.Tif = "D"; //day order
                        stiOrder.PriceType = SterlingLib.STIPriceTypes.ptSTILmt;
                        stiOrder.LmtPrice = Convert.ToDouble(testAlgo.midPrice + (testAlgo.incrementPrice * 5));
                        stiOrder.Destination = "BATS";
                        stiOrder.ClOrderID = Guid.NewGuid().ToString();

                        int orderStatus = stiOrder.SubmitOrder();
                        if (orderStatus != 0)
                        {
                            MessageBox.Show("Order Error: " + orderStatus.ToString());
                        }
                        else
                        {
                            //Add to appropriate list
                            testAlgo.ordersAbove.Add(stiOrder);
                        }

                        //Cancel the "6th" order from opposite side order list

                        SterlingLib.ISTIOrder cancelOrder = testAlgo.ordersBelow[testAlgo.ordersBelow.Count - 1];
                        orderMaint.CancelOrder(Globals.account, 0, cancelOrder.ClOrderID, Guid.NewGuid().ToString());
                        testAlgo.ordersBelow.RemoveAt(testAlgo.ordersBelow.Count - 1);

                        //Place new order in opposite side order list

                        stiOrder = new SterlingLib.STIOrder();
                        stiOrder.Symbol = testAlgo.symbol;
                        stiOrder.Account = Globals.account;
                        stiOrder.Side = "B";
                        stiOrder.Quantity = testAlgo.incrementSize;
                        stiOrder.Tif = "D"; //day order
                        stiOrder.PriceType = SterlingLib.STIPriceTypes.ptSTILmt;
                        stiOrder.LmtPrice = Convert.ToDouble(testAlgo.midPrice - (testAlgo.incrementPrice));
                        stiOrder.Destination = "BATS";
                        stiOrder.ClOrderID = Guid.NewGuid().ToString();

                        orderStatus = stiOrder.SubmitOrder();
                        if (orderStatus != 0)
                        {
                            MessageBox.Show("Order Error: " + orderStatus.ToString());
                        }
                        else
                        {
                            //Add to appropriate list
                            testAlgo.ordersBelow.Add(stiOrder);
                        }
                    }

                    
                }
            }



        }


        private void button1_Click(object sender, EventArgs e)
        {
            testAlgo.status = "Running";

            decimal startPrice = Convert.ToDecimal(numericUpDown1.Value);  //Also midprice

            testAlgo.symbol = textBox1.Text.ToUpper();
            testAlgo.incrementPrice = numericUpDown8.Value;
            testAlgo.incrementSize = Convert.ToInt32(numericUpDown10.Value);

            testAlgo.Start(startPrice);

            Thread.Sleep(200);

            Debug.WriteLine("Buy Fills: " + testAlgo.buyFills);
            Debug.WriteLine("Sell Fills: " + testAlgo.sellFills);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Stop

            //Cancel all orders in ordersabove, below, set algo status to stopped

            foreach (SterlingLib.ISTIOrder order in testAlgo.ordersAbove)
            {
                orderMaint.CancelOrder(Globals.account, 0, order.ClOrderID, Guid.NewGuid().ToString());
            }
            foreach (SterlingLib.ISTIOrder order in testAlgo.ordersBelow)
            {
                orderMaint.CancelOrder(Globals.account, 0, order.ClOrderID, Guid.NewGuid().ToString());
            }

            testAlgo.status = "Stopped";

        }
    }
}
