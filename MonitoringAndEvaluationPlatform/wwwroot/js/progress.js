//function updateProgressBars() {
//    debugger;
//    document.querySelectorAll('.dynamic-progress').forEach(element => {
//        const value = parseFloat(element.dataset.value);
//        const progressBar = element.querySelector('.progress-bar');
//        const valueElement = element.querySelector('.metric-value');

//        let barClass, valueClass;

//        if (value > 80) {
//            barClass = 'progress-success';
//            valueClass = 'value-success';
//        } else if (value > 60) {
//            barClass = 'progress-info';
//            valueClass = 'value-info';
//        } else if (value > 40) {
//            barClass = 'progress-warning';
//            valueClass = 'value-warning';
//        } else if (value > 20) {
//            barClass = 'progress-orange';
//            valueClass = 'value-orange';
//        } else {
//            barClass = 'progress-danger';
//            valueClass = 'value-danger';
//        }

//        progressBar.className = 'progress-bar ' + barClass;
//        progressBar.style.width = value + '%';

//        if (valueElement) {
//            valueElement.className = 'metric-value ' + valueClass;
//            valueElement.textContent = value + '%';
//        }
//    });
//}

//// Initialize on page load
//document.addEventListener('DOMContentLoaded', updateProgressBars);