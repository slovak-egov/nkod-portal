import Alert from './Alert';

export default function AlertPublisher() {
    return (
        <Alert type="warning" style={{ padding: '10px' }}>
            Datasety vložené do 15.1.2024 budú vymazané!
        </Alert>
    );
}
