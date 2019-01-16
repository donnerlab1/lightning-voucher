const uri = "api/voucher";


$(document).ready(function() {
	$("#payment-div").hide();
	$("#claim-div").hide();
  getData();
});

function useVoucher() {
	const use_item= {
		voucher_id :$("#voucher-id").val(),
		pay_req : $("#voucher-payreq").val()
	};
	$.ajax({
		type: "GET",
		url: uri + "/pay/" + use_item.voucher_id + "/" + use_item.pay_req,
		cache: false,
		success: function(data) {
			console.log(data);
			$("#payment-div").show();
			const tBody = $("#payment-table");	
			$(tBody).empty();
			console.log(window.btoa(data.paymentPreimage))
			const tr = $("<tr></tr>")
					.append($("<td></td>").text(data.paymentError))
					.append($("<td></td>").text(window.btoa(data.paymentPreimage)))
          
				;
			
			tr.appendTo(tBody);

		},
		error: function(jqXHR, textStatus, errorThrown) {
			
		}

	});

}
function _arrayBufferToBase64( buffer ) {
	var binary = '';
	var bytes = new Uint8Array( buffer );
	var len = bytes.byteLength;
	for (var i = 0; i < len; i++) {
		binary += String.fromCharCode( bytes[ i ] );
	}
	return window.btoa( binary );
}

function buyVoucher() {
	const buy_item= {
		buy_amt :$("#buy_amt").val(),
		buy_sat : $("#buy_sat").val()
	}
	$.ajax({
		type: "GET",
		url: uri + "/buy/" + buy_item.buy_amt + "/" + buy_item.buy_sat,
		cache: false,
		success: function(data) {
			console.log(data);
			$("#buy-invoice-stuff").remove();
			textfield = $("#buy-invoice-text");
			textfield.show();
			textfield.append("<span id='buy-invoice-stuff'>"+data.paymentRequest+"</span>");
			$("#voucher-buy-payreq").val(data.paymentRequest);

		},
		error: function(jqXHR, textStatus, errorThrown) {
			
			textfield = $("#buy-invoice-text");
			textfield.show();
		}

	});
}

function claimVoucher() {
	const claim_item = {
		claim_payreq: $("#voucher-buy-payreq").val()
	};
	$.ajax({
		type: "GET",
		url: uri + "/claim/" + claim_item.claim_payreq,
		cache: false,
		success: function(data) {
			console.log(data);
			if (data.errorCode === "Ok") {
				$("#claim-div").show();
				const tBody = $("#voucher-table");
				$(tBody).empty();


				$.each(data.vouchers,
					function(key, item) {
						const tr = $("<tr></tr>")
							.append($("<td></td>").text(item.id))
							.append($("<td></td>").text(restSat));
						tr.appendTo(tBody);
					});
			} else {
				$("#claim-div").show();
				const tBody = $("#voucher-table");
				$(tBody).empty();
				const tr = $("<tr></tr>")
					.append($("<td></td>").text(data.errorCode));

				tr.appendTo(tBody);
			}
		},
		error: function(jqXHR, textStatus, errorThrown) {
			$("#claim-div").show();
		}

	});
}
function getData() {
  $.ajax({
    type: "GET",
    url: uri,
    cache: false,
    success: function(data) {
      const tBody = $("#voucher-table");
		
	    $("#claim-div").show();
	  console.log(data)
      $(tBody).empty();


      $.each(data, function(key, item) {
	      restSat = item.startSat - item.usedSat;
        const tr = $("<tr></tr>")
          
          .append($("<td></td>").text(item.id))
		  .append($("<td></td>").text(restSat))
          
          ;

        tr.appendTo(tBody);
      });

    }
  });
}



