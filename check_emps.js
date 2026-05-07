const sql = require('mssql');
const config = {
    user: 'sa',
    password: 'SuperStrong!Passw0rd',
    server: 'localhost',
    database: 'GPMS_Desktop',
    options: { encrypt: false, trustServerCertificate: true }
};
async function query() {
    try {
        await sql.connect(config)
        let emps = await sql.query("SELECT * FROM [Employee];")
        console.table(emps.recordset)
    } catch (err) {
        console.error("SQL Error", err)
    } finally {
        process.exit();
    }
}
query()
