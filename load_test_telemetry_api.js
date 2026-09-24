// Array of target metrics
const metricTypes = ['pressure', 'temperature', 'vibration', 'flow_rate', 'voltage'];

// Pick random values
const randomMetric = metricTypes[Math.floor(Math.random() * metricTypes.length)];
const randomDeviceId = `sensor-node-${Math.floor(Math.random() * 1000) + 1}`;
const randomValue = parseFloat((Math.random() * 150.0).toFixed(2));
const currentTimestamp = new Date().toISOString();

// Set collection/environment variables for request body injection
pm.variables.set("deviceId", randomDeviceId);
pm.variables.set("metricName", randomMetric);
pm.variables.set("value", randomValue);
pm.variables.set("timestamp", currentTimestamp);