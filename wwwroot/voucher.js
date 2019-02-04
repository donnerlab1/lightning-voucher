const uri = "api/voucher";


$(document).ready(function() {
	$("#payment-div").hide();

	$("#get-voucher-div").hide();
	$("#decode-invoice-button").click(function() {
		decodePayment();
	});
	$("#buylink").attr("href", window.location.origin+"/voucher/");
	getVoucher();
	//getData();
});

function decodePayment() {
	const use_item = {
		voucher_id: $("#voucher-id").val(),
		pay_req: $("#voucher-payreq").val()
	};
	$.ajax({
		type: "GET",
		url: uri + "/decode/" + use_item.pay_req,
		cache: false,
		success: function (data) {
			console.log(data);
			$("#decode-invoice-stuff").remove();
			textfield = $("#decode-invoice-text");
			textfield.show();
			textfield.append("<span id='decode-invoice-stuff'>" +"Amount: "+ data.numSatoshis+" satoshi Destination: "+ data.destination + "<br>Description: "+ data.description+"</span>");
			$("#voucher-buy-payreq").val(data.paymentRequest);

		},
		error: function (jqXHR, textStatus, errorThrown) {

		    $("#payment-div").hide();
		    $("#decode-invoice-stuff").remove();
		    textfield = $("#decode-invoice-text");
		    textfield.show();
		    textfield.append("<span id='decode-invoice-stuff' style='color:red'>INVALID INVOICE</span>");
		}
	});
}
function getVoucher() {
	const use_item = {
		voucher_id: $("#voucher-id").text(),
		pay_req: $("#voucher-payreq").val()
	};
	$.ajax({
		type: "GET",
		url: uri + "/" + use_item.voucher_id,
		cache: false,
		success: function (data) {
			console.log(data);
			$("#get-voucher-div").show();
			const tBody = $("#get-voucher-table");
			$(tBody).empty();
			restSat = data.startSat - data.usedSat;
			const tr = $("<tr></tr>")
				.append($("<td></td>").text(data.id))
				.append($("<td></td>").text(restSat))
				.append($("<td></td>").text(data.startSat));
			tr.appendTo(tBody);

		},
		error: function (jqXHR, textStatus, errorThrown) {
			$("#get-voucher-div").show();
			const tBody = $("#get-voucher-table");
			$(tBody).empty();
			const tr = $("<tr></tr>")
				.append($("<td style='color:red'></td>").text("VOUCHER NOT FOUND OR SPENT"))
			tr.appendTo(tBody);
		}
	});
}
function useVoucher() {
    $("#decode-invoice-stuff").remove();
    const voucher_button = $("#use-voucher-button");
    voucher_button.attr("disabled", "disabled")
    voucher_button.attr('value', "Please Wait...");
	textfield = $("#decode-invoice-text");
	textfield.show();
	textfield.append("<span id='decode-invoice-stuff' style='color:red'>PAYING, Standby</span>");
	const use_item= {
		voucher_id: $("#voucher-id").text(),
		pay_req : $("#voucher-payreq").val()
	};
	$.ajax({
		type: "GET",
		url: uri + "/pay/" + use_item.voucher_id + "/" + use_item.pay_req,
		cache: false,
		success: function (data) {
		    voucher_button.removeAttr("disabled");
		    voucher_button.attr('value', "Pay");
			$("#decode-invoice-stuff").remove();
			console.log(data);
			$("#payment-div").show();
			const tBody = $("#payment-table");	
			$(tBody).empty();
			const tr = $("<tr></tr>");
			if (data.paymentError === "")
				data.paymentError = "Payment sent!"
			if (data.paymentRoute == null) {
				
						tr.append($("<td></td>").text(data.paymentError))


					;
			} else {
			    voucher_button.removeAttr("disabled");
			    voucher_button.attr('value', "Pay");
						tr.append($("<td></td>").text(data.paymentError))

						.append($("<td></td>").text(data.paymentRoute.totalAmt))
						.append($("<td></td>").text(data.paymentRoute.totalFees))
                         
						    .append($("<td></td>").text(data.paymentPreimage))
						getVoucher();
			}
			
			
			tr.appendTo(tBody);

		},
		error: function(jqXHR, textStatus, errorThrown) {
		    voucher_button.removeAttr("disabled");
		    voucher_button.attr('value', "Pay");
		    $("#decode-invoice-stuff").remove();
		    textfield = $("#decode-invoice-text");
		    textfield.show();
		    textfield.append("<span id='decode-invoice-stuff' style='color:red'>INVALID INVOICE</span>");

		    $("#payment-div").hide();
		}

	});

}




