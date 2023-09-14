import Alert from "./Alert";

export default function Loading() {
    return <Alert type="info">
        <div style={{padding: '5px 10px'}}>
            Načítavam údaje...
        </div>
    </Alert>;
}