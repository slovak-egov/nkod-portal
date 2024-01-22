import Alert from './Alert';

export default function AlertPublisher() {
    return (
        <Alert type="warning" style={{ padding: '10px' }}>
            Datasety vložené do 5.2.2024 budú vymazané!
        </Alert>
    );
}
