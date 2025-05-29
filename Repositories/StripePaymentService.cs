//  // Logica per il pagamento PayPal
//         if (confirmPaypalBtn) {
//             confirmPaypalBtn.addEventListener('click', async () => {
//                 confirmPaypalBtn.disabled = true;
//                 confirmPaypalBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processando...';

//                 const itemsInCart = [];
//                 document.querySelectorAll('#carrello-items-body tr').forEach(row => {
//                     const quantityInput = row.querySelector('.item-quantity-input');
//                     if (quantityInput) {
//                         const productId = parseInt(quantityInput.dataset.productId);
//                         const quantity = parseInt(quantityInput.value);
//                         if (quantity > 0) {
//                             itemsInCart.push({ ProdottoId: productId, Quantita: quantity });
//                         }
//                     }
//                 });

//                 if (itemsInCart.length === 0) {
//                     alert("Il carrello è vuoto. Impossibile procedere con il pagamento.");
//                     confirmPaypalBtn.disabled = false;
//                     confirmPaypalBtn.textContent = 'Conferma con PayPal';
//                     return;
//                 }
                
//                 let currentTotalPriceNum = 0;
//                 if (totalPriceElement && totalPriceElement.textContent) {
//                     currentTotalPriceNum = parseFloat(totalPriceElement.textContent.replace('€', '').replace(',', '.')) || 0;
//                 }

//                 try {
//                     // Prima crea l'ordine con stato "In attesa di pagamento PayPal"
//                     const orderResponse = await fetch('/Carrello/ConfermaOrdine', {
//                         method: 'POST',
//                         headers: {
//                             'Content-Type': 'application/json',
//                             'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
//                         },
//                         body: JSON.stringify({
//                             ArticoliOrdine: itemsInCart,
//                             PaymentMethodId: 'paypal-pending'
//                         })
//                     });

//                     const orderResult = await orderResponse.json();

//                     if (!orderResponse.ok || !orderResult.orderId) {
//                         alert(orderResult.messaggio || 'Errore durante la creazione preliminare dell\'ordine per PayPal.');
//                         confirmPaypalBtn.disabled = false;
//                         confirmPaypalBtn.textContent = 'Conferma con PayPal';
//                         return;
//                     }
                    
//                     // Procedi con il pagamento PayPal
//                     const paypalResponse = await fetch('/Carrello/ProcessPaypalPayment', {
//                         method: 'POST',
//                         headers: {
//                             'Content-Type': 'application/json',
//                             'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
//                         },
//                         body: JSON.stringify({
//                             orderId: orderResult.orderId,
//                             totalAmount: currentTotalPriceNum
//                         })
//                     });
                    
//                     const paypalResult = await paypalResponse.json();
//                     if (paypalResponse.ok && paypalResult.redirectUrl) {
//                         window.location.href = paypalResult.redirectUrl;
//                     } else {
//                         alert(paypalResult.message || 'Errore durante la preparazione del pagamento PayPal.');
//                         confirmPaypalBtn.disabled = false;
//                         confirmPaypalBtn.textContent = 'Conferma con PayPal';
//                     }
//                 } catch (error) {
//                     alert('Errore di comunicazione con il server per PayPal.');
//                     console.error("PayPal Error:", error);
//                     confirmPaypalBtn.disabled = false;
//                     confirmPaypalBtn.textContent = 'Conferma con PayPal';
//                 }
//             });
//         }

//         // Logica per il bonifico bancario
//         if (confirmBankTransferBtn) {
//             confirmBankTransferBtn.addEventListener('click', async () => {
//                 if (!confirm('Confermi di voler procedere con un ordine con pagamento tramite bonifico bancario? Riceverai i dettagli per il pagamento dopo la conferma.')) {
//                     return;
//                 }
                
//                 confirmBankTransferBtn.disabled = true;
//                 confirmBankTransferBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processando...';

//                 const itemsInCart = [];
//                 document.querySelectorAll('#carrello-items-body tr').forEach(row => {
//                     const quantityInput = row.querySelector('.item-quantity-input');
//                     if (quantityInput) {
//                         const productId = parseInt(quantityInput.dataset.productId);
//                         const quantity = parseInt(quantityInput.value);
//                         if (quantity > 0) {
//                             itemsInCart.push({ ProdottoId: productId, Quantita: quantity });
//                         }
//                     }
//                 });

//                 if (itemsInCart.length === 0) {
//                     alert("Il carrello è vuoto. Impossibile procedere con l'ordine.");
//                     confirmBankTransferBtn.disabled = false;
//                     confirmBankTransferBtn.textContent = 'Conferma Ordine con Bonifico';
//                     return;
//                 }

//                 try {
//                     const response = await fetch('/Carrello/ConfermaOrdine', {
//                         method: 'POST',
//                         headers: {
//                             'Content-Type': 'application/json',
//                             'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
//                         },
//                         body: JSON.stringify({
//                             ArticoliOrdine: itemsInCart,
//                             PaymentMethodId: 'bank-transfer'
//                         })
//                     });
                    
//                     const result = await response.json();
//                     if (response.ok && result.messaggio) {
//                         if (orderNumberSpan) orderNumberSpan.textContent = result.orderNumber;
//                         alert('Ordine creato con successo! Il tuo ordine numero ' + result.orderNumber + ' è in attesa di bonifico. Riceverai una mail con i dettagli.');
//                         if (paymentModal) paymentModal.style.display = "none";
//                         window.location.href = '/Ordini/MieiOrdini';
//                     } else {
//                         alert(result.messaggio || 'Errore durante la creazione dell\'ordine con bonifico.');
//                     }
//                 } catch (error) {
//                     alert('Errore di comunicazione con il server per il bonifico bancario.');
//                     console.error("Bank Transfer Error:", error);
//                 } finally {
//                     confirmBankTransferBtn.disabled = false;
//                     confirmBankTransferBtn.textContent = 'Conferma Ordine con Bonifico';
//                 }
//             });
//         }