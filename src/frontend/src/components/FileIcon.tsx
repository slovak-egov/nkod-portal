import plainIcon from '../icons/plain.png';

type Props = {
    format: string;
};

export default function FileIcon(props: Props) {
    return (
        <div
            style={{
                backgroundImage: 'url(' + plainIcon + ')',
                backgroundRepeat: 'no-repeat',
                backgroundPosition: 'top center',
                backgroundSize: 'contain',
                minWidth: '55px',
                lineHeight: '20px',
                padding: '25px 0 10px 5px',
                textAlign: 'center',
                fontSize: '12px',
                marginRight: '20px'
            }}
        >
            {props.format.length > 3 ? props.format.substring(0, 3) : props.format}
        </div>
    );
}
